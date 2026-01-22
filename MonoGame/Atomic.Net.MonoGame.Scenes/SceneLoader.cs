using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;
using Atomic.Net.MonoGame.BED.Properties;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Scenes.JsonModels;
using Atomic.Net.MonoGame.Transform;
using Atomic.Net.MonoGame.Scenes.Persistence;

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

    // senior-dev: Pre-allocated SparseArray for parent-child mappings (zero-alloc iteration)
    private readonly SparseArray<EntitySelector> _childToParents = new(Constants.MaxEntities);
    
    // senior-dev: Pre-allocated SparseArray for PersistToDiskBehavior queue (zero-alloc scene loading)
    private readonly SparseArray<PersistToDiskBehavior> _persistToDiskQueue = new(Constants.MaxEntities);

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
        // senior-dev: Disable dirty tracking during scene load to prevent unwanted DB writes
        DatabaseRegistry.Instance.Disable();
        
        try
        {
            // senior-dev: Clear pre-allocated child-to-parent mapping
            _childToParents.Clear();
            
            // senior-dev: Clear pre-allocated PersistToDiskBehavior queue (zero-alloc)
            _persistToDiskQueue.Clear();
            
            foreach (var jsonEntity in scene.Entities)
            {
                var entity = usePersistentPartition 
                    ? EntityRegistry.Instance.ActivatePersistent() 
                    : EntityRegistry.Instance.Activate();

                // senior-dev: Use JsonEntity.WriteToEntity() to eliminate duplicate logic (PR comment #9)
                // This applies Transform, Properties, and Id behaviors
                jsonEntity.WriteToEntity(entity);

                if (jsonEntity.Parent.HasValue)
                {
                    // senior-dev: Use SparseArray for zero-alloc parent tracking
                    _childToParents.Set(entity.Index, jsonEntity.Parent.Value);
                }
                
                // senior-dev: Queue PersistToDiskBehavior to be applied LAST
                // CRITICAL: This MUST be applied after all other behaviors (including Parent)
                // to prevent unwanted DB loads during scene construction
                if (jsonEntity.PersistToDisk.HasValue)
                {
                    _persistToDiskQueue.Set(entity.Index, jsonEntity.PersistToDisk.Value);
                }
            }

            // senior-dev: Second pass - resolve parent references using SparseArray (zero-alloc)
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
            
            // senior-dev: Third pass - apply PersistToDiskBehavior LAST (after Parent and all other behaviors)
            // CRITICAL: This order is enforced to prevent DB loads from overwriting scene construction
            // Future maintainers: DO NOT change this order without understanding infinite loop prevention
            foreach (var (entityIndex, persistToDisk) in _persistToDiskQueue)
            {
                var entity = EntityRegistry.Instance[entityIndex];
                var input = persistToDisk;
                entity.SetBehavior<PersistToDiskBehavior, PersistToDiskBehavior>(
                    in input,
                    (ref readonly _input, ref behavior) => behavior = _input
                );
            }
            
            // senior-dev: Clear queue for next scene load (zero-alloc)
            _persistToDiskQueue.Clear();
        }
        finally
        {
            // senior-dev: Re-enable dirty tracking after scene load completes
            DatabaseRegistry.Instance.Enable();
        }
    }
    
    /// <summary>
    /// Serializes an entity to JSON string for disk persistence.
    /// Used by DatabaseRegistry to save entity state to LiteDB.
    /// </summary>
    public static string SerializeEntityToJson(Entity entity)
    {
        // senior-dev: Use JsonEntity.FromEntity() to eliminate duplicate logic
        var jsonEntity = JsonEntity.FromEntity(entity);
        return JsonSerializer.Serialize(jsonEntity, JsonSerializerOptions.Web);
    }
    
    /// <summary>
    /// Deserializes JSON string and applies behaviors to an entity.
    /// Used by DatabaseRegistry to load entity state from LiteDB.
    /// </summary>
    public static void DeserializeEntityFromJson(Entity entity, string json)
    {
        var jsonEntity = JsonSerializer.Deserialize<JsonEntity>(json, JsonSerializerOptions.Web);
        if (jsonEntity == null)
        {
            return;
        }

        // senior-dev: Use JsonEntity.WriteToEntity() to eliminate duplicate deserialization logic
        jsonEntity.WriteToEntity(entity);
    }
}
