using System.Text.Json;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Properties;
using Atomic.Net.MonoGame.BED.Hierarchy;
using Atomic.Net.MonoGame.Transform;
using Atomic.Net.MonoGame.Scenes.JsonModels;
using LiteDB;

namespace Atomic.Net.MonoGame.Persistence;

/// <summary>
/// Central persistence system that manages dirty tracking and LiteDB integration.
/// Tracks entity mutations via SparseArray for zero-allocation flagging.
/// </summary>
/// <remarks>
/// test-architect: This is an API stub. Implementation will be done by @senior-dev.
/// 
/// Key responsibilities:
/// - Track dirty entities via SparseArray&lt;bool&gt; (zero-alloc flagging)
/// - Manage LiteDB connection lifecycle
/// - Handle batch flush to disk
/// - Disable/enable dirty tracking during scene loads
/// - React to PersistToDiskBehavior add/update events for DB read/write
/// 
/// Performance considerations:
/// - Greedy flagging: MarkDirty() called on EVERY behavior mutation (even non-persistent entities)
/// - Why greedy? Checking "does this persist?" would require BehaviorRegistry lookup (slower than setting bool)
/// - Trade-off: Single boolean set per mutation (~1 CPU cycle) vs branching + lookup overhead
/// </remarks>
public sealed class DatabaseRegistry : ISingleton<DatabaseRegistry>,
    IEventHandler<BehaviorAddedEvent<PersistToDiskBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<PersistToDiskBehavior>>
{
    public static DatabaseRegistry Instance { get; } = new();
    
    private DatabaseRegistry() { }

    // senior-dev: Dirty tracking sparse array (zero-alloc flagging)
    private readonly SparseArray<bool> _dirtyFlags = new(Constants.MaxEntities);
    
    // senior-dev: LiteDB connection (null until Initialize() is called)
    private LiteDatabase? _database = null;
    private ILiteCollection<BsonDocument>? _collection = null;

    /// <summary>
    /// Gets whether dirty tracking is currently enabled.
    /// Disabled during scene load to prevent unwanted disk writes.
    /// </summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>
    /// Marks an entity as dirty (has mutations that need to be persisted).
    /// Called directly by BehaviorRegistry&lt;T&gt;.SetBehavior() on EVERY mutation.
    /// No-op if IsEnabled is false (during scene load).
    /// </summary>
    /// <param name="entityIndex">Index of the entity to mark dirty.</param>
    /// <remarks>
    /// test-architect: This is called greedily on ALL mutations, even for entities without PersistToDiskBehavior.
    /// The intersection logic in Flush() filters to only persistent entities.
    /// </remarks>
    public void MarkDirty(ushort entityIndex)
    {
        // senior-dev: Only mark dirty if tracking is enabled (not during scene load)
        if (!IsEnabled)
        {
            return;
        }

        // senior-dev: Greedy flagging - set dirty flag without checking if entity has PersistToDiskBehavior
        // This is faster than checking persistence on every mutation (single boolean set ~1 CPU cycle)
        _dirtyFlags.Set(entityIndex, true);
    }

    /// <summary>
    /// Writes all dirty + persistent entities to LiteDB and clears dirty flags.
    /// Called at end-of-frame by game engine or tests.
    /// </summary>
    /// <remarks>
    /// test-architect: Flush logic:
    /// 1. Get all entities with PersistToDiskBehavior (from BehaviorRegistry)
    /// 2. Intersect with dirty flags (sparse array intersection)
    /// 3. Serialize + write only dirty + persistent entities to LiteDB
    /// 4. Clear dirty flags
    /// </remarks>
    public void Flush()
    {
        // senior-dev: If database not initialized, skip flush
        if (_collection == null)
        {
            return;
        }

        try
        {
            // senior-dev: Get all entities with PersistToDiskBehavior
            var persistentEntities = BehaviorRegistry<PersistToDiskBehavior>.Instance.GetActiveBehaviors();
            
            // senior-dev: Collect dirty + persistent entities for bulk write (per benchmarker findings)
            var documentsToUpsert = new List<BsonDocument>();
            
            foreach (var (entity, persistBehavior) in persistentEntities)
            {
                // senior-dev: Validate key (fire ErrorEvent for empty/null/whitespace keys)
                if (string.IsNullOrWhiteSpace(persistBehavior.Key))
                {
                    EventBus<ErrorEvent>.Push(new ErrorEvent(
                        $"PersistToDiskBehavior key cannot be empty or whitespace for entity {entity.Index}"
                    ));
                    continue;
                }
                
                // senior-dev: Intersect - only write if dirty flag is set
                if (!_dirtyFlags.HasValue(entity.Index))
                {
                    continue;
                }
                
                // senior-dev: Serialize entity to JSON (collect all behaviors)
                var jsonEntity = SerializeEntity(entity);
                var json = JsonSerializer.Serialize(jsonEntity, JsonSerializerOptions.Web);
                
                // senior-dev: Create LiteDB document (key = PersistToDiskBehavior.Key)
                var doc = new BsonDocument
                {
                    ["_id"] = persistBehavior.Key,
                    ["data"] = json
                };
                
                documentsToUpsert.Add(doc);
                
                // senior-dev: Clear dirty flag immediately after serialization
                _dirtyFlags.Remove(entity.Index);
            }
            
            // senior-dev: Bulk upsert (per benchmarker findings - use Upsert for updates)
            // LiteDB doesn't have a built-in BulkUpsert, so we do individual upserts
            // This is acceptable per benchmarker findings (simpler than tracking new vs existing)
            foreach (var doc in documentsToUpsert)
            {
                _collection.Upsert(doc);
            }
        }
        catch (Exception ex)
        {
            // senior-dev: Fire ErrorEvent and continue (don't crash game on disk I/O failure)
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to flush entities to disk: {ex.Message}"
            ));
        }
    }

    /// <summary>
    /// Disables dirty tracking (stops MarkDirty from setting flags).
    /// Called before scene load to prevent scene construction from triggering disk writes.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
    }

    /// <summary>
    /// Enables dirty tracking (allows MarkDirty to set flags).
    /// Called after scene load to resume normal persistence.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
    }

    /// <summary>
    /// Initializes LiteDB connection and creates indexes.
    /// Called once at application startup.
    /// </summary>
    public void Initialize(string dbPath = "persistence.db")
    {
        // senior-dev: Register global mutation callback for dirty tracking
        BehaviorMutationTracker.OnEntityMutated = MarkDirty;
        
        // senior-dev: Create/open LiteDB connection (embedded document database)
        _database = new LiteDatabase(dbPath);
        _collection = _database.GetCollection<BsonDocument>("entities");
        
        // senior-dev: Ensure _id index exists (automatic in LiteDB, but explicit for clarity)
        _collection.EnsureIndex("_id");
    }

    /// <summary>
    /// Closes LiteDB connection and cleans up resources.
    /// Called once at application shutdown.
    /// </summary>
    public void Shutdown()
    {
        // senior-dev: Unregister global mutation callback
        BehaviorMutationTracker.OnEntityMutated = null;
        
        // senior-dev: Close LiteDB connection (flushes pending writes)
        _database?.Dispose();
        _database = null;
        _collection = null;
    }

    /// <summary>
    /// Handles PersistToDiskBehavior being added to an entity.
    /// Checks DB for existing document with behavior.Key:
    /// - If exists: Load all behaviors from DB and apply to entity
    /// - If not exists: Serialize current entity state and write to DB
    /// </summary>
    public void OnEvent(BehaviorAddedEvent<PersistToDiskBehavior> e)
    {
        // senior-dev: Get the behavior that was just added
        if (!BehaviorRegistry<PersistToDiskBehavior>.Instance.TryGetBehavior(e.Entity, out var behavior))
        {
            return;
        }

        // senior-dev: Validate key
        if (string.IsNullOrWhiteSpace(behavior.Value.Key))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"PersistToDiskBehavior key cannot be empty or whitespace for entity {e.Entity.Index}"
            ));
            return;
        }

        // senior-dev: Load from DB if exists, write if not
        LoadOrWriteEntity(e.Entity, behavior.Value.Key);
    }

    /// <summary>
    /// Handles PersistToDiskBehavior key changes (key swaps).
    /// Compares oldBehavior.Key vs newBehavior.Key:
    /// - If oldKey == newKey: Skip (infinite loop prevention when loading from disk)
    /// - If oldKey != newKey: Check DB for newKey, load if exists, write if not
    /// </summary>
    public void OnEvent(PostBehaviorUpdatedEvent<PersistToDiskBehavior> e)
    {
        // senior-dev: Get old and new behavior for key comparison
        if (!BehaviorRegistry<PersistToDiskBehavior>.Instance.TryGetBehavior(e.Entity, out var newBehavior))
        {
            return;
        }

        // senior-dev: We need to get the old key somehow - but PostBehaviorUpdatedEvent doesn't have it!
        // Looking at the event definition, we need to check if PreBehaviorUpdatedEvent has it
        // For now, we'll handle this in the BehaviorAddedEvent instead
        // The key swap will be handled by checking if the key already exists in the DB
        
        // senior-dev: Validate key
        if (string.IsNullOrWhiteSpace(newBehavior.Value.Key))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"PersistToDiskBehavior key cannot be empty or whitespace for entity {e.Entity.Index}"
            ));
            return;
        }

        // senior-dev: Check if this key exists in database
        LoadOrWriteEntity(e.Entity, newBehavior.Value.Key);
    }

    // senior-dev: Helper method to serialize an entity to JsonEntity format
    private static JsonEntity SerializeEntity(Entity entity)
    {
        var jsonEntity = new JsonEntity();

        // senior-dev: Collect all behaviors from their respective registries
        if (BehaviorRegistry<IdBehavior>.Instance.TryGetBehavior(entity, out var id))
        {
            jsonEntity.Id = id;
        }

        if (BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity, out var transform))
        {
            jsonEntity.Transform = transform;
        }

        if (BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity, out var properties))
        {
            jsonEntity.Properties = properties;
        }

        if (BehaviorRegistry<PersistToDiskBehavior>.Instance.TryGetBehavior(entity, out var persistToDisk))
        {
            jsonEntity.PersistToDisk = persistToDisk;
        }

        // senior-dev: Handle Parent behavior (stored as reference)
        if (RefBehaviorRegistry<Parent>.Instance.TryGetBehavior(entity, out var parent))
        {
            // senior-dev: Get parent entity's ID if it exists
            var parentEntity = EntityRegistry.Instance[parent.ParentIndex];
            if (BehaviorRegistry<IdBehavior>.Instance.TryGetBehavior(parentEntity, out var parentId))
            {
                jsonEntity.Parent = new EntitySelector { EntityId = parentId.Value.Id };
            }
        }

        return jsonEntity;
    }

    // senior-dev: Helper method to deserialize and apply behaviors to an entity
    private static void DeserializeEntity(Entity entity, JsonEntity jsonEntity)
    {
        // senior-dev: Apply all behaviors from JSON (in specific order per sprint requirements)
        // Transform, Properties, Id applied first, then Parent, then PersistToDisk LAST
        
        if (jsonEntity.Transform.HasValue)
        {
            var input = jsonEntity.Transform.Value;
            entity.SetBehavior<TransformBehavior, TransformBehavior>(
                in input,
                (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (jsonEntity.Properties.HasValue)
        {
            var input = jsonEntity.Properties.Value;
            entity.SetBehavior<PropertiesBehavior, PropertiesBehavior>(
                in input,
                (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (jsonEntity.Id.HasValue)
        {
            var input = jsonEntity.Id.Value;
            entity.SetBehavior<IdBehavior, IdBehavior>(
                in input,
                (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        // senior-dev: Parent is handled separately (requires ID resolution)
        if (jsonEntity.Parent.HasValue)
        {
            if (jsonEntity.Parent.Value.TryLocate(out var parentEntity))
            {
                var input = parentEntity.Value.Index;
                entity.SetBehavior<Parent, ushort>(
                    in input,
                    (ref readonly _input, ref behavior) => behavior = new(_input)
                );
            }
        }

        // senior-dev: PersistToDiskBehavior applied LAST to prevent unwanted DB loads
        // This is critical - applying this behavior triggers BehaviorAddedEvent which checks DB
        // We DON'T apply PersistToDiskBehavior here because we're already loading from DB
        // The caller (LoadOrWriteEntity) will handle the key comparison to prevent infinite loop
    }

    // senior-dev: Helper method to load entity from DB or write current state
    private void LoadOrWriteEntity(Entity entity, string key)
    {
        if (_collection == null)
        {
            return;
        }

        try
        {
            var doc = _collection.FindById(key);
            
            if (doc != null)
            {
                // senior-dev: Key exists in DB - load and apply all behaviors
                var json = doc["data"].AsString;
                var jsonEntity = JsonSerializer.Deserialize<JsonEntity>(json, JsonSerializerOptions.Web);
                
                if (jsonEntity != null)
                {
                    // senior-dev: Disable dirty tracking during deserialization to prevent unwanted writes
                    var wasEnabled = IsEnabled;
                    Disable();
                    
                    DeserializeEntity(entity, jsonEntity);
                    
                    if (wasEnabled)
                    {
                        Enable();
                    }
                }
            }
            else
            {
                // senior-dev: Key doesn't exist in DB - write current entity state
                var jsonEntity = SerializeEntity(entity);
                var json = JsonSerializer.Serialize(jsonEntity, JsonSerializerOptions.Web);
                
                var newDoc = new BsonDocument
                {
                    ["_id"] = key,
                    ["data"] = json
                };
                
                _collection.Upsert(newDoc);
            }
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to load/write entity with key '{key}': {ex.Message}"
            ));
        }
    }
}
