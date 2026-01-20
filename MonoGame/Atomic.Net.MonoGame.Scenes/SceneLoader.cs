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

    /// <summary>
    /// Loads a game scene from JSON file.
    /// Spawns entities using EntityRegistry.Activate() (indices ≥32).
    /// Fires ErrorEvent for file not found, invalid JSON, or unresolved references.
    /// </summary>
    public void LoadGameScene(string scenePath)
    {
        LoadSceneInternal(scenePath, useLoadingPartition: false);
    }

    /// <summary>
    /// Loads a loading scene from JSON file.
    /// Spawns entities using EntityRegistry.ActivateLoading() (indices &lt;32).
    /// Fires ErrorEvent for file not found, invalid JSON, or unresolved references.
    /// </summary>
    public void LoadLoadingScene(string scenePath = "Content/Scenes/loading.json")
    {
        LoadSceneInternal(scenePath, useLoadingPartition: true);
    }

    private void LoadSceneInternal(string scenePath, bool useLoadingPartition)
    {
        if (!File.Exists(scenePath))
        {
            EventBus<ErrorEvent>.Push(new($"Scene file not found: {scenePath}"));
            return;
        }

        JsonScene scene;
        try
        {
            var jsonText = File.ReadAllText(scenePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new TransformBehaviorConverter() }
            };
            scene = JsonSerializer.Deserialize<JsonScene>(jsonText, options) ?? new JsonScene();
        }
        catch (JsonException ex)
        {
            EventBus<ErrorEvent>.Push(new($"Failed to parse JSON scene file: {scenePath} - {ex.Message}"));
            return;
        }

        var createdEntities = new SparseArray<bool>(Constants.MaxEntities);
        var parentIds = new Dictionary<ushort, string>();

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
                parentIds[entity.Index] = jsonEntity.Parent;
            }

            createdEntities.Set(entity.Index, true);
        }

        foreach (var (entityIndex, _) in createdEntities)
        {
            if (!parentIds.TryGetValue(entityIndex, out var parentRef))
            {
                continue;
            }

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

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
