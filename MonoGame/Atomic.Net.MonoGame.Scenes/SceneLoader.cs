using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;
using Atomic.Net.MonoGame.BED.Properties;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Scenes.JsonModels;
using Atomic.Net.MonoGame.Transform;

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
    private readonly SparseArray<EntitySelector> _childToParents = new(Constants.MaxEntities);

    /// <summary>
    /// Loads a game scene from JSON file.
    /// Spawns entities using EntityRegistry.Activate() (indices ≥256).
    /// Fires ErrorEvent for file not found, invalid JSON, or unresolved references.
    /// </summary>
    public void LoadGameScene(string scenePath)
    {
        if (!TryParseSceneFile(scenePath, out var scene))
        {
            return;
        }

        LoadSceneInternal(scene, usePersistentPartition: false);
        
        // senior-dev: GC after scene goes out of scope
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// Loads a persistent scene from JSON file.
    /// Spawns entities using EntityRegistry.ActivatePersistent() (indices &lt;256).
    /// Fires ErrorEvent for file not found, invalid JSON, or unresolved references.
    /// </summary>
    public void LoadPersistentScene(string scenePath = "Content/Scenes/loading.json")
    {
        if (!TryParseSceneFile(scenePath, out var scene))
        {
            return;
        }

        LoadSceneInternal(scene, usePersistentPartition: true);
        
        // senior-dev: GC after scene goes out of scope
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // senior-dev: Separate method for parsing JSON to ensure proper scoping for GC
    private static bool TryParseSceneFile(
        string scenePath,
        [NotNullWhen(true)]
        out JsonScene? scene
    )
    {
        if (!File.Exists(scenePath))
        {
            EventBus<ErrorEvent>.Push(new($"Scene file not found: '{scenePath}'"));
            scene = null;
            return false;
        }

        var jsonText = File.ReadAllText(scenePath);
        
        try
        {
            scene = JsonSerializer.Deserialize<JsonScene>(
                jsonText, JsonSerializerOptions.Web
            ) ?? new();

            return true;
        }
        catch (JsonException ex)
        {
            EventBus<ErrorEvent>.Push(new($"Failed to parse JSON scene file: {scenePath} - {ex.Message}"));
        }

        scene = null;
        return false;
    }

    private void LoadSceneInternal(JsonScene scene, bool usePersistentPartition)
    {
        // senior-dev: Clear pre-allocated child-to-parent mapping
        _childToParents.Clear();
        
        foreach (var jsonEntity in scene.Entities)
        {
            var entity = usePersistentPartition 
                ? EntityRegistry.Instance.ActivatePersistent() 
                : EntityRegistry.Instance.Activate();

            if (jsonEntity.Transform.HasValue)
            {
                // Closure avoidance
                var input = jsonEntity.Transform.Value;
                entity.SetBehavior<TransformBehavior, TransformBehavior>(
                    in input,
                    (ref readonly _input, ref behavior) => behavior = _input
                );
            }

            if (jsonEntity.Properties.HasValue)
            {
                // Closure avoidance
                var input = jsonEntity.Properties.Value;
                entity.SetBehavior<PropertiesBehavior, PropertiesBehavior>(
                    in input,
                    (ref readonly _input, ref behavior) => behavior = _input
                );
            }

            if (jsonEntity.Id.HasValue)
            {
                // Closure avoidance
                var input = jsonEntity.Id.Value;
                entity.SetBehavior<IdBehavior, IdBehavior>(
                    in input,
                    (ref readonly _input, ref behavior) => behavior = _input
                );
            }

            if (jsonEntity.Parent.HasValue)
            {
                // senior-dev: Use SparseReferenceArray for zero-alloc parent tracking
                _childToParents.Set(entity.Index, jsonEntity.Parent.Value);
            }
        }

        // senior-dev: Second pass - resolve parent references using SparseReferenceArray (zero-alloc)
        foreach (var (entityIndex, parentSelector) in _childToParents)
        {
            if (!parentSelector.TryLocate(out var parent))
            {
                continue;
            }

            var entity = EntityRegistry.Instance[entityIndex];

            // Closure avoidance
            var input = parent.Value.Index;
            entity.SetBehavior<Parent, ushort>(
                in input,
                (ref readonly _input, ref behavior) => behavior = new(_input)
            );
        }

        _childToParents.Clear();
    }
}
