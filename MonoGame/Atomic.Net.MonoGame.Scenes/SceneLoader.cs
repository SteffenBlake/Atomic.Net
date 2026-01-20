using System.Text.Json;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Scenes.JsonModels;
using Atomic.Net.MonoGame.Transform;
using Atomic.Net.MonoGame.Transform.JsonConverters;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Singleton scene loader that loads scenes from JSON files.
/// Uses two-pass loading:
///   - Pass 1: Create all entities, apply behaviors (Transform, IdBehavior)
///   - Pass 2: Resolve parent references, fire error events for unresolved IDs
/// Forces GC after loading completes to prevent lag in game scene.
/// </summary>
public sealed class SceneLoader : ISingleton<SceneLoader>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
    }

    public static SceneLoader Instance { get; private set; } = null!;

    // senior-dev: Pre-allocated SparseReferenceArray for parent-child mappings (zero-alloc iteration)
    private readonly SparseReferenceArray<string> _childToParents = new(Constants.MaxEntities);
    
    // senior-dev: Reusable JSON options with attribute-based converters
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = 
        { 
            new Core.JsonConverters.Vector3Converter(),
            new Core.JsonConverters.QuaternionConverter(),
            new Transform.JsonConverters.TransformBehaviorConverter()
        }
    };

    /// <summary>
    /// Loads a game scene from JSON file.
    /// Spawns entities using EntityRegistry.Activate() (indices ≥32).
    /// Fires ErrorEvent for file not found, invalid JSON, or unresolved references.
    /// </summary>
    public void LoadGameScene(string scenePath)
    {
        var scene = ParseSceneFile(scenePath);
        if (scene == null)
        {
            return;
        }

        LoadSceneInternal(scene, useLoadingPartition: false);
        
        // senior-dev: GC after scene goes out of scope
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// Loads a loading scene from JSON file.
    /// Spawns entities using EntityRegistry.ActivateLoading() (indices &lt;32).
    /// Fires ErrorEvent for file not found, invalid JSON, or unresolved references.
    /// </summary>
    public void LoadLoadingScene(string scenePath = "Content/Scenes/loading.json")
    {
        var scene = ParseSceneFile(scenePath);
        if (scene == null)
        {
            return;
        }

        LoadSceneInternal(scene, useLoadingPartition: true);
        
        // senior-dev: GC after scene goes out of scope
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // senior-dev: Separate method for parsing JSON to ensure proper scoping for GC
    private JsonScene? ParseSceneFile(string scenePath)
    {
        if (!File.Exists(scenePath))
        {
            EventBus<ErrorEvent>.Push(new($"Scene file not found: {scenePath}"));
            return null;
        }

        try
        {
            var jsonText = File.ReadAllText(scenePath);
            return JsonSerializer.Deserialize<JsonScene>(jsonText, _jsonOptions) ?? new JsonScene();
        }
        catch (JsonException ex)
        {
            EventBus<ErrorEvent>.Push(new($"Failed to parse JSON scene file: {scenePath} - {ex.Message}"));
            return null;
        }
    }

    private void LoadSceneInternal(JsonScene scene, bool useLoadingPartition)
    {
        // senior-dev: Clear pre-allocated child-to-parent mapping
        _childToParents.Clear();
        
        var createdEntities = new SparseArray<bool>(Constants.MaxEntities);

        foreach (var jsonEntity in scene.Entities)
        {
            var entity = useLoadingPartition 
                ? EntityRegistry.Instance.ActivateLoading() 
                : EntityRegistry.Instance.Activate();

            if (jsonEntity.Transform.HasValue)
            {
                BehaviorRegistry<TransformBehavior>.Instance.SetBehavior(
                    entity, 
                    (ref TransformBehavior behavior) => behavior = jsonEntity.Transform.Value
                );
            }

            if (!string.IsNullOrEmpty(jsonEntity.Id))
            {
                BehaviorRegistry<IdBehavior>.Instance.SetBehavior(
                    entity, 
                    (ref IdBehavior behavior) => behavior = new IdBehavior(jsonEntity.Id)
                );
            }

            if (!string.IsNullOrEmpty(jsonEntity.Parent))
            {
                // senior-dev: Use SparseReferenceArray for zero-alloc parent tracking
                _childToParents[entity.Index] = jsonEntity.Parent;
            }

            createdEntities.Set(entity.Index, true);
        }

        // senior-dev: Second pass - resolve parent references using SparseReferenceArray (zero-alloc)
        foreach (var (entityIndex, parentRef) in _childToParents)
        {
            var parentId = parentRef.TrimStart('#');
            var entity = EntityRegistry.Instance[entityIndex];
            
            if (EntityIdRegistry.Instance.TryResolve(parentId, out var parentEntity))
            {
                BehaviorRegistry<Parent>.Instance.SetBehavior(
                    entity, 
                    (ref Parent behavior) => behavior = new Parent(parentEntity.Index)
                );
            }
            else
            {
                EventBus<ErrorEvent>.Push(new($"Unresolved parent reference: #{parentId}"));
            }
        }
    }
}
