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
    private readonly Queue<string> _errorQueue = new();
    private readonly object _errorQueueLock = new();

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

        // Reset progress counters
        Interlocked.Exchange(ref _bytesRead, 0);
        Interlocked.Exchange(ref _totalBytes, 0);
        Interlocked.Exchange(ref _entitiesLoaded, 0);
        Interlocked.Exchange(ref _totalEntities, 0);
        LoadingProgress = 0.0f;

        // Start background task
        Task.Run(() => LoadSceneAsync(scenePath));
    }

    private async Task LoadSceneAsync(string scenePath)
    {
        // senior-dev: FINDING: EntityRegistry.Activate() and other registry methods are NOT thread-safe
        // for concurrent calls from background thread. This implementation calls them from background
        // thread as specified in sprint requirements, but this causes race conditions in practice.
        // Future work: Either make registries thread-safe OR marshal entity spawning back to main thread.
        // For now, error handling and progress tracking work correctly, but entity spawning may fail
        // intermittently due to thread safety issues.

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

            // Reset scene partition (clear scene entities/rules/sequences)
            ResetDriver.Instance.Run();

            // Read and deserialize JSON
            JsonScene? scene;
            try
            {
                using var fileStream = new FileStream(scenePath, FileMode.Open, FileAccess.Read);
                var progress = new Progress<long>(bytesRead =>
                {
                    Interlocked.Exchange(ref _bytesRead, bytesRead);
                });

                using var progressStream = new ProgressStream(fileStream, progress);
                scene = await JsonSerializer.DeserializeAsync<JsonScene>(progressStream, _serializerOptions);

                if (scene == null)
                {
                    QueueError($"Failed to parse scene JSON: {scenePath} - deserializer returned null");
                    ReturnToIdle();
                    return;
                }
            }
            catch (JsonException ex)
            {
                QueueError($"Failed to parse scene JSON: {scenePath} - {ex.Message}");
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
    }

    /// <summary>
    /// Updates loading progress and processes GC requests.
    /// Must be called each frame from main thread.
    /// </summary>
    public void Update()
    {
        // Process error queue
        lock (_errorQueueLock)
        {
            while (_errorQueue.Count > 0)
            {
                var error = _errorQueue.Dequeue();
                EventBus<ErrorEvent>.Push(new ErrorEvent(error));
            }
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

        // Calculate progress: 66% file I/O + 33% entity spawning
        var fileProgress = totalBytes > 0 ? (float)bytesRead / totalBytes : 0.0f;
        var entityProgress = totalEntities > 0 ? (float)entitiesLoaded / totalEntities : 0.0f;
        LoadingProgress = 0.66f * fileProgress + 0.33f * entityProgress;
    }

    private void QueueError(string message)
    {
        lock (_errorQueueLock)
        {
            _errorQueue.Enqueue(message);
        }
    }

    private void ReturnToIdle()
    {
        Interlocked.Exchange(ref _isLoading, 0);
        LoadingProgress = 1.0f;
    }
}
