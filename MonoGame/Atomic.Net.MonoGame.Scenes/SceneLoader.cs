using System.Text.Json;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Scenes.JsonConverters;
using Atomic.Net.MonoGame.Scenes.JsonModels;
using Atomic.Net.MonoGame.Transform;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Singleton scene loader that loads scenes from JSON files.
/// Uses two-pass loading:
///   - Pass 1: Create all entities, apply behaviors (Transform, EntityId)
///   - Pass 2: Resolve parent references, fire error events for unresolved IDs
/// Forces GC after loading completes to prevent lag in game scene.
/// </summary>
public sealed class SceneLoader : ISingleton<SceneLoader>
{
    private static SceneLoader? _instance;
    public static SceneLoader Instance => _instance ??= new SceneLoader();

    private SceneLoader()
    {
        // senior-dev: Ensure required systems are initialized
        // senior-dev: This is safe to call multiple times (Initialize() methods are idempotent)
        SceneSystem.Initialize();
    }

    /// <summary>
    /// Loads a game scene from JSON file.
    /// Spawns entities using EntityRegistry.Activate() (indices ≥32).
    /// Fires ErrorEvent for file not found, invalid JSON, or unresolved references.
    /// </summary>
    public void LoadGameScene(string scenePath)
    {
        // test-architect: Stub - To be implemented by #senior-dev
        // senior-dev: Using EntityRegistry.Activate() for game scene entities (indices >= 32)
        LoadScene(scenePath, useLoadingPartition: false);
    }

    /// <summary>
    /// Loads a loading scene from JSON file.
    /// Spawns entities using EntityRegistry.ActivateLoading() (indices &lt;32).
    /// Fires ErrorEvent for file not found, invalid JSON, or unresolved references.
    /// </summary>
    public void LoadLoadingScene(string scenePath)
    {
        // test-architect: Stub - To be implemented by #senior-dev
        // senior-dev: Using EntityRegistry.ActivateLoading() for loading scene entities (indices < 32)
        LoadScene(scenePath, useLoadingPartition: true);
    }

    private void LoadScene(string scenePath, bool useLoadingPartition)
    {
        // senior-dev: Check if file exists
        if (!File.Exists(scenePath))
        {
            EventBus<ErrorEvent>.Push(new($"Scene file not found: {scenePath}"));
            return;
        }

        JsonScene scene;
        try
        {
            // senior-dev: Read and deserialize JSON file
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
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new($"Error loading scene file: {scenePath} - {ex.Message}"));
            return;
        }

        // senior-dev: Track created entities for Pass 2
        var createdEntities = new List<(Entity entity, JsonEntity jsonEntity)>();

        // senior-dev: PASS 1 - Create all entities and apply behaviors (Transform, EntityId)
        foreach (var jsonEntity in scene.Entities)
        {
            try
            {
                // senior-dev: Activate entity in appropriate partition
                var entity = useLoadingPartition 
                    ? EntityRegistry.Instance.ActivateLoading() 
                    : EntityRegistry.Instance.Activate();

                // senior-dev: Apply Transform behavior if present
                if (jsonEntity.Transform.HasValue)
                {
                    BehaviorRegistry<TransformBehavior>.Instance.SetBehavior(
                        entity, 
                        (ref TransformBehavior behavior) => behavior = jsonEntity.Transform.Value
                    );
                }

                // senior-dev: Apply EntityId behavior if present
                if (!string.IsNullOrEmpty(jsonEntity.Id))
                {
                    BehaviorRegistry<EntityId>.Instance.SetBehavior(
                        entity, 
                        (ref EntityId behavior) => behavior = new EntityId(jsonEntity.Id)
                    );
                }

                createdEntities.Add((entity, jsonEntity));
            }
            catch (Exception ex)
            {
                EventBus<ErrorEvent>.Push(new($"Error creating entity: {ex.Message}"));
            }
        }

        // senior-dev: PASS 2 - Resolve parent references
        foreach (var (entity, jsonEntity) in createdEntities)
        {
            if (string.IsNullOrEmpty(jsonEntity.Parent))
            {
                continue;
            }

            try
            {
                // senior-dev: Strip '#' prefix from parent reference
                var parentId = jsonEntity.Parent.TrimStart('#');
                
                if (EntityIdRegistry.Instance.TryResolve(parentId, out var parentEntity))
                {
                    // senior-dev: Set Parent behavior to establish relationship
                    BehaviorRegistry<Parent>.Instance.SetBehavior(
                        entity, 
                        (ref Parent behavior) => behavior = new Parent(parentEntity.Index)
                    );
                }
                else
                {
                    // senior-dev: Fire error event for unresolved reference (don't crash)
                    EventBus<ErrorEvent>.Push(new($"Unresolved parent reference: #{parentId}"));
                }
            }
            catch (Exception ex)
            {
                EventBus<ErrorEvent>.Push(new($"Error resolving parent reference: {ex.Message}"));
            }
        }

        // senior-dev: Force GC to clean up ephemeral JSON objects
        // senior-dev: Only acceptable because this happens at load time, not during gameplay
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
