using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Hierarchy;
using Atomic.Net.MonoGame.Persistence;
using Atomic.Net.MonoGame.Selectors;

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
        LoadSceneInternal(scenePath, useGlobalPartition: false);
        
        // GC after scene goes out of scope
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// Loads a global scene from JSON file.
    /// Spawns entities using EntityRegistry.ActivateGlobal() (indices &lt;256).
    /// Fires ErrorEvent for file not found, invalid JSON, or unresolved references.
    /// </summary>
    public void LoadGlobalScene(string scenePath = "Content/Scenes/loading.json")
    {
        LoadSceneInternal(scenePath, useGlobalPartition: true);
        
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

    private void LoadSceneInternal(string scenePath, bool useGlobalPartition)
    {
        if (!TryParseSceneFile(scenePath, out var scene))
        {
            return;
        }

        // We do NOT disable dirty tracking during scene load to prevent unwanted DB writes here
        // An "outer" calling system will be in charge of calling this
        
        // Clear pre-allocated PersistToDiskBehavior queue (zero-alloc)
        _persistToDiskQueue.Clear();
        
        foreach (var jsonEntity in scene.Entities)
        {
            var entity = useGlobalPartition 
                ? EntityRegistry.Instance.ActivateGlobal() 
                : EntityRegistry.Instance.Activate();

            // Use JsonEntity.WriteToEntity() to eliminate duplicate logic (PR comment #9)
            // This applies Transform, Properties, Id, and Parent behaviors
            jsonEntity.WriteToEntity(entity);
            
            // CRITICAL: PersistToDiskBehavior MUST be applied after all other behaviors (including Parent)
            // to prevent unwanted DB loads during scene construction
            if (jsonEntity.PersistToDisk.HasValue)
            {
                _persistToDiskQueue.Set(entity.Index, jsonEntity.PersistToDisk.Value);
            }
        }
        
        // CRITICAL: Apply PersistToDiskBehavior LAST (after Parent and all other behaviors)
        // This order prevents DB loads from overwriting scene construction
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
        
        // Clear queue for next scene load (zero-alloc)
        _persistToDiskQueue.Clear();
        
        // test-architect: Load rules into RuleRegistry - to be implemented by @senior-dev
        // LoadRules(scene, useGlobalPartition);
        
        // CRITICAL: Recalc selectors first, then hierarchy
        // SelectorRegistry.Recalc() must run before HierarchyRegistry.Recalc()
        // to ensure parent selectors have valid Matches arrays
        SelectorRegistry.Instance.Recalc();
        HierarchyRegistry.Instance.Recalc();
    }
    
    /// <summary>
    /// Loads rules from scene into RuleRegistry.
    /// </summary>
    private void LoadRules(JsonScene scene, bool useGlobalPartition)
    {
        // test-architect: Stub implementation - to be implemented by @senior-dev
        throw new NotImplementedException("SceneLoader.LoadRules() - Rule loading logic not yet implemented");
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
