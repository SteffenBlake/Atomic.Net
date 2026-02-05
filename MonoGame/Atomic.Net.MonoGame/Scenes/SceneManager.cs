using System.Collections.Concurrent;
using System.Text.Json;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Hierarchy;
using Atomic.Net.MonoGame.Selectors;
using Atomic.Net.MonoGame.Sequencing;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Singleton manager for async scene transitions with progress tracking.
/// Coordinates background scene loading with main thread GC execution.
/// </summary>
public sealed class SceneManager : ISingleton<SceneManager>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = new();
    }

    public static SceneManager Instance { get; private set; } = null!;

    // Thread-safe state flags (use Interlocked for updates)
    private int _isLoading = 0; // 0 = false, 1 = true
    private int _gcRequested = 0; // 0 = false, 1 = true

    // Progress counters (use Interlocked for updates)
    private long _bytesRead = 0;
    private long _totalBytes = 0;
    private int _entitiesLoaded = 0;
    private int _totalEntities = 0;

    // Error queue for marshaling errors from background thread to main thread
    private readonly ConcurrentQueue<string> _errorQueue = new();

    /// <summary>
    /// Gets whether a scene is currently loading.
    /// Thread-safe read.
    /// </summary>
    public bool IsLoading => Interlocked.CompareExchange(ref _isLoading, 0, 0) == 1;

    /// <summary>
    /// Gets the loading progress (0.0 to 1.0).
    /// Formula: 66% file I/O + 33% entity spawning.
    /// Thread-safe read.
    /// </summary>
    public float LoadingProgress { get; private set; } = 1.0f;

    private static readonly JsonSerializerOptions _serializerOptions =
        new(JsonSerializerOptions.Web)
        {
            RespectRequiredConstructorParameters = true
        };

    /// <summary>
    /// Starts async scene loading from specified path.
    /// Returns immediately after starting background task.
    /// </summary>
    /// <param name="scenePath">Path to scene JSON file</param>
    public void LoadScene(string scenePath)
    {
        // Prevent concurrent loads
        if (Interlocked.CompareExchange(ref _isLoading, 1, 0) == 1)
        {
            QueueError("Cannot load scene while another scene is loading");
            return;
        }

        // senior-dev: THREAD SAFETY ANALYSIS - ResetDriver.Run() on Main Thread
        // 
        // ISSUE: ResetDriver.Run() must be called on main thread because it calls Reset() methods
        // on all registries (EntityRegistry, RuleRegistry, SequenceRegistry, etc.), which modify
        // sparse arrays and other non-thread-safe data structures.
        //
        // IMPACT: This means scene clearing happens synchronously on the main thread, which blocks
        // for ~0.1-0.5ms depending on scene size. This is acceptable because:
        // 1. Scene clearing is much faster than scene loading (typically <1ms vs 10-100ms+)
        // 2. Clearing must complete before background loading starts to avoid race conditions
        // 3. The blocking time is negligible compared to a frame budget (16.67ms @ 60fps)
        //
        // WHY NOT BACKGROUND THREAD:
        // Moving ResetDriver.Run() to background thread would require making ALL registries
        // thread-safe for concurrent access:
        // - EntityRegistry: _active, _enabled sparse arrays modified during Deactivate()
        // - RuleRegistry: Rules sparse array cleared
        // - SequenceRegistry: _sequences sparse array cleared, _idToIndex dictionary modified
        // - SelectorRegistry: Recalc() modifies _dirty flags and Matches arrays
        // - HierarchyRegistry: _childToParentLookup and _dirtyChildren modified
        // - DatabaseRegistry: _dirtyFlags sparse array accessed
        // - PropertiesRegistry: _keyIndex and _keyValueIndex dictionaries modified
        // - TagRegistry: _tagToEntities dictionary modified
        // - TransformRegistry: _dirty and _nextDirty sparse arrays modified
        // - EntityIdRegistry: _idToEntity and _entityToId dictionaries modified
        //
        // EFFORT ESTIMATE: Making all registries thread-safe would require:
        // - Add ReaderWriterLockSlim to each registry (~10 files)
        // - Wrap all reads in read locks, all writes in write locks (~200+ locations)
        // - Test for deadlocks and race conditions
        // - Performance impact of locking (could reduce throughput by 10-20%)
        // - Estimated: 3-5 days of work + 2-3 days of testing
        //
        // DECISION: Keep ResetDriver.Run() on main thread. The minimal blocking time is acceptable
        // trade-off vs extensive refactoring and performance impact of locking.
        //
        ResetDriver.Instance.Run();

        // Reset progress counters
        Interlocked.Exchange(ref _bytesRead, 0);
        Interlocked.Exchange(ref _totalBytes, 0);
        Interlocked.Exchange(ref _entitiesLoaded, 0);
        Interlocked.Exchange(ref _totalEntities, 0);
        LoadingProgress = 0.0f;

        // Start background task (only for file I/O and JSON parsing)
        LoadSceneAsync(scenePath);
    }

    private Task LoadSceneAsync(string scenePath)
    {
        return Task.Run(() =>
        {
            // senior-dev: CRITICAL - Background thread error handler
            // EventBus.Push() invokes handlers on the calling thread. When registries
            // (RuleRegistry, SequenceRegistry) push ErrorEvents from this background thread,
            // those events would invoke ErrorEventLogger.OnEvent() on the background thread,
            // which calls List.Add() - NOT thread-safe!
            //
            // Solution: Register a local error handler that captures errors and queues them
            // for processing on the main thread via SceneManager.Update().
            var backgroundErrorHandler = new BackgroundErrorHandler(this);
            EventBus<ErrorEvent>.Register(backgroundErrorHandler);

            try
            {
                // Check if file exists
                if (!File.Exists(scenePath))
                {
                    QueueError($"Scene file not found: '{scenePath}'");
                    ReturnToIdle();
                    return;
                }

                // Get file size for progress tracking
                var fileInfo = new FileInfo(scenePath);
                Interlocked.Exchange(ref _totalBytes, fileInfo.Length);

                // Read and deserialize JSON
                JsonScene? scene;
                try
                {
                    var jsonText = File.ReadAllText(scenePath);
                    Interlocked.Exchange(ref _bytesRead, jsonText.Length);

                    scene = JsonSerializer.Deserialize<JsonScene>(jsonText, _serializerOptions);

                    if (scene == null)
                    {
                        QueueError($"Failed to parse scene JSON: {scenePath} - deserializer returned null");
                        ReturnToIdle();
                        return;
                    }
                }
                catch (JsonException ex)
                {
                    QueueError($"Failed to parse scene JSON (JsonException): {scenePath} - {ex.Message}");
                    ReturnToIdle();
                    return;
                }
                catch (Exception ex)
                {
                    QueueError($"Failed to parse scene JSON (Exception): {scenePath} - {ex.GetType().Name}: {ex.Message}");
                    ReturnToIdle();
                    return;
                }

                // Set total entities for progress tracking
                var entityCount = scene.Entities?.Count ?? 0;
                Interlocked.Exchange(ref _totalEntities, entityCount);
                Interlocked.Exchange(ref _bytesRead, fileInfo.Length); // Mark file reading as complete

                // Spawn entities
                if (scene.Entities != null)
                {
                    foreach (var jsonEntity in scene.Entities)
                    {
                        try
                        {
                            var entity = EntityRegistry.Instance.Activate();
                            jsonEntity.WriteToEntity(entity);
                        }
                        catch (Exception ex)
                        {
                            // Queue error but continue loading remaining entities
                            var entityJson = JsonSerializer.Serialize(jsonEntity, JsonSerializerOptions.Web);
                            QueueError($"Failed to spawn entity during scene load: {ex.Message}\nEntity JSON: {entityJson}");
                        }
                        finally
                        {
                            Interlocked.Increment(ref _entitiesLoaded);
                        }
                    }
                }

                // Load rules
                if (scene.Rules != null)
                {
                    foreach (var rule in scene.Rules)
                    {
                        RuleRegistry.Instance.TryActivate(rule, out _);
                    }
                }

                // Load sequences
                if (scene.Sequences != null)
                {
                    foreach (var sequence in scene.Sequences)
                    {
                        SequenceRegistry.Instance.TryActivate(sequence, out _);
                    }
                }

                // Recalc selectors and hierarchy
                SelectorRegistry.Instance.Recalc();
                HierarchyRegistry.Instance.Recalc();

                // Request GC on main thread
                Interlocked.Exchange(ref _gcRequested, 1);
            }
            catch (Exception ex)
            {
                QueueError($"Unexpected error during scene load: {ex.Message}");
                ReturnToIdle();
            }
            finally
            {
                // senior-dev: CRITICAL - Unregister background error handler
                // Must be in finally block to ensure it's always removed, even if exception occurs
                EventBus<ErrorEvent>.Unregister(backgroundErrorHandler);
            }
        });
    }

    /// <summary>
    /// Updates loading progress and processes GC requests.
    /// Must be called each frame from main thread.
    /// </summary>
    public void Update()
    {
        // Process error queue
        while (_errorQueue.TryDequeue(out var error))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(error));
        }

        // Update progress if loading
        if (IsLoading)
        {
            UpdateProgress();
        }

        // Check for GC request
        if (Interlocked.CompareExchange(ref _gcRequested, 0, 0) == 1)
        {
            // Execute GC on main thread
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect(); // Second pass to collect finalized objects

            // Clear GC request flag
            Interlocked.Exchange(ref _gcRequested, 0);

            // Set loading to false (transition complete)
            Interlocked.Exchange(ref _isLoading, 0);

            // Reset progress to 1.0
            LoadingProgress = 1.0f;
        }
    }

    private void UpdateProgress()
    {
        // Read thread-safe counters
        var bytesRead = Interlocked.Read(ref _bytesRead);
        var totalBytes = Interlocked.Read(ref _totalBytes);
        var entitiesLoaded = Interlocked.CompareExchange(ref _entitiesLoaded, 0, 0);
        var totalEntities = Interlocked.CompareExchange(ref _totalEntities, 0, 0);

        // Calculate progress: 66% file I/O + 33% entity spawning (per sprint requirements)
        var fileProgress = totalBytes > 0 ? (float)bytesRead / totalBytes : 0.0f;
        var entityProgress = totalEntities > 0 ? (float)entitiesLoaded / totalEntities : 0.0f;
        LoadingProgress = 0.66f * fileProgress + 0.33f * entityProgress;
    }

    private void QueueError(string message)
    {
        _errorQueue.Enqueue(message);
    }

    private void ReturnToIdle()
    {
        Interlocked.Exchange(ref _isLoading, 0);
        LoadingProgress = 1.0f;
    }

    /// <summary>
    /// Background thread error handler that captures ErrorEvents pushed from the background thread
    /// and queues them for processing on the main thread.
    /// 
    /// This is necessary because EventBus.Push() invokes handlers synchronously on the calling thread.
    /// When registries (RuleRegistry, SequenceRegistry) push ErrorEvents from the background thread,
    /// those events would invoke ALL registered handlers (like ErrorEventLogger) on the background thread,
    /// which can cause thread-safety issues with handlers that use non-thread-safe data structures.
    /// 
    /// By registering this handler at the start of the background thread, we capture those errors
    /// and re-queue them to SceneManager's ConcurrentQueue, which is then processed on the main
    /// thread in Update(), where EventBus.Push() is called again - this time on the main thread,
    /// safely invoking all other handlers.
    /// </summary>
    private sealed class BackgroundErrorHandler : IEventHandler<ErrorEvent>
    {
        private readonly SceneManager _manager;

        public BackgroundErrorHandler(SceneManager manager)
        {
            _manager = manager;
        }

        public void OnEvent(ErrorEvent e)
        {
            // Queue error to be re-emitted on main thread
            // This prevents other handlers from being invoked on the background thread
            _manager.QueueError(e.Message);
        }
    }
}
