using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Hierarchy;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Persistence;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Selectors;
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
    
    // Pre-allocated SparseArray for PersistToDiskBehavior queue (zero-alloc scene loading)
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
        
        // GC after scene goes out of scope
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
        
        // GC after scene goes out of scope
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // Separate method for parsing JSON to ensure proper scoping for GC
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
        // We do NOT disable dirty tracking during scene load to prevent unwanted DB writes here
        // An "outer" calling system will be in charge of calling this
        
        // Clear pre-allocated PersistToDiskBehavior queue (zero-alloc)
        _persistToDiskQueue.Clear();
        
        // senior-dev: Two-pass loading to fix selector timing issue (code-reviewer feedback)
        // PASS 1: Create entities and apply Transform, Properties, Id behaviors
        // This creates selectors and marks them dirty but doesn't try to use them yet
        foreach (var jsonEntity in scene.Entities)
        {
            var entity = usePersistentPartition 
                ? EntityRegistry.Instance.ActivatePersistent() 
                : EntityRegistry.Instance.Activate();

            // Apply Transform
            if (jsonEntity.Transform.HasValue)
            {
                var input = jsonEntity.Transform.Value;
                entity.SetBehavior<TransformBehavior, TransformBehavior>(
                    in input,
                    (ref readonly _input, ref behavior) => behavior = _input
                );
            }

            // Apply Properties
            if (jsonEntity.Properties.HasValue)
            {
                var input = jsonEntity.Properties.Value;
                entity.SetBehavior<PropertiesBehavior, PropertiesBehavior>(
                    in input,
                    (ref readonly _input, ref behavior) => behavior = _input
                );
            }

            // Apply Id (creates selectors)
            if (jsonEntity.Id.HasValue)
            {
                var input = jsonEntity.Id.Value;
                entity.SetBehavior<IdBehavior, IdBehavior>(
                    in input,
                    (ref readonly _input, ref behavior) => behavior = _input
                );
            }
            
            // Queue PersistToDisk for later (must be applied LAST)
            if (jsonEntity.PersistToDisk.HasValue)
            {
                _persistToDiskQueue.Set(entity.Index, jsonEntity.PersistToDisk.Value);
            }
        }
        
        // senior-dev: CRITICAL - Recalc BEFORE applying Parent behaviors
        // This ensures selector Matches arrays are populated so parent lookups work
        // See code-reviewer feedback: timing was wrong when Recalc was at the end
        SelectorRegistry.Instance.Recalc();
        
        // PASS 2: Apply Parent behaviors (now that selectors are recalculated)
        var entityIndex = usePersistentPartition ? (ushort)0 : Constants.MaxPersistentEntities;
        foreach (var jsonEntity in scene.Entities)
        {
            if (jsonEntity.Parent.HasValue)
            {
                var entity = EntityRegistry.Instance[entityIndex];
                var input = jsonEntity.Parent.Value;
                entity.SetBehavior<ParentBehavior, ParentBehavior>(
                    in input,
                    (ref readonly _input, ref behavior) => behavior = _input
                );
            }
            entityIndex++;
        }
        
        // CRITICAL: Apply PersistToDiskBehavior LAST (after Parent and all other behaviors)
        // This order prevents DB loads from overwriting scene construction
        // Future maintainers: DO NOT change this order without understanding infinite loop prevention
        foreach (var (entityIdx, persistToDisk) in _persistToDiskQueue)
        {
            var entity = EntityRegistry.Instance[entityIdx];
            var input = persistToDisk;
            entity.SetBehavior<PersistToDiskBehavior, PersistToDiskBehavior>(
                in input,
                (ref readonly _input, ref behavior) => behavior = _input
            );
        }
        
        // Clear queue for next scene load (zero-alloc)
        _persistToDiskQueue.Clear();
    }
  
    // Static instance to write to, to avoid alloc'ing a new one each time
    private static JsonEntity _jsonInstance = new();
    /// <summary>
    /// Serializes an entity to JSON string for disk persistence.
    /// Used by DatabaseRegistry to save entity state to LiteDB.
    /// </summary>
    public static string SerializeEntityToJson(Entity entity)
    {
        // Use JsonEntity.FromEntity() to eliminate duplicate logic
        JsonEntity.ReadFromEntity(entity, ref _jsonInstance);
        return JsonSerializer.Serialize(_jsonInstance, JsonSerializerOptions.Web);
    }
    
    /// <summary>
    /// Deserializes JSON string and applies behaviors to an entity.
    /// Used by DatabaseRegistry to load entity state from LiteDB.
    /// </summary>
    public static void DeserializeEntityFromJson(Entity entity, string json)
    {
        // SteffenBlake: #benchmarker - Benchmarked this approach vs streaming (Utf8JsonReader + pooling).
        // RESULT: Current approach is 20-21% FASTER than streaming approaches.
        // The intermediate JsonEntity allocation (~32 bytes) is negligible compared to the
        // performance benefit of using the highly optimized JsonSerializer.
        // Streaming with Utf8JsonReader is slower and adds complexity with NO memory benefit.
        // See: MonoGame/Atomic.Net.MonoGame.Benchmarks/Persistence/Units/JSON_ENTITY_DESERIALIZATION_RESULTS.md
        // Verdict: Keep this implementation. It's optimal.
        var jsonEntity = JsonSerializer.Deserialize<JsonEntity>(json, JsonSerializerOptions.Web);
        if (jsonEntity == null)
        {
            return;
        }

        // Use JsonEntity.WriteToEntity() to eliminate duplicate deserialization logic
        jsonEntity.WriteToEntity(entity);
    }
}
