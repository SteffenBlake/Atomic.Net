using Atomic.Net.MonoGame.Core;
using LiteDB;

namespace Atomic.Net.MonoGame.BED.Persistence;

/// <summary>
/// Central persistence system that manages dirty tracking and LiteDB integration.
/// Tracks entity mutations via SparseArray for zero-allocation flagging.
/// </summary>
/// <remarks>
/// senior-dev: DatabaseRegistry is in BED to avoid circular dependencies.
/// It works with a simple key-value store (key -> JSON string).
/// The JSON serialization/deserialization is handled by the caller (e.g., SceneLoader).
/// </remarks>
public sealed class DatabaseRegistry : ISingleton<DatabaseRegistry>,
    IEventHandler<BehaviorAddedEvent<PersistToDiskBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<PersistToDiskBehavior>>
{
    public static DatabaseRegistry Instance { get; } = new();
    
    private DatabaseRegistry() { }

    // senior-dev: Dirty tracking sparse array (zero-alloc flagging)
    private readonly SparseArray<bool> _dirtyFlags = new(Constants.MaxEntities);
    
    // senior-dev: Pre-allocated list for batch upsert operations (zero-alloc flush)
    private readonly List<BsonDocument> _documentsToUpsert = new(Constants.MaxEntities);
    
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
    /// Called via BehaviorMutationTracker callback on EVERY mutation.
    /// No-op if IsEnabled is false (during scene load).
    /// </summary>
    /// <param name="entityIndex">Index of the entity to mark dirty.</param>
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
    /// Called once at application startup or in test setup.
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
        
        // senior-dev: Register for PersistToDiskBehavior events
        EventBus<BehaviorAddedEvent<PersistToDiskBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<PersistToDiskBehavior>>.Register(this);
    }

    /// <summary>
    /// Closes LiteDB connection and cleans up resources.
    /// Called once at application shutdown or in test cleanup.
    /// </summary>
    public void Shutdown()
    {
        // senior-dev: Unregister global mutation callback
        BehaviorMutationTracker.OnEntityMutated = null;
        
        // senior-dev: Unregister event handlers
        EventBus<BehaviorAddedEvent<PersistToDiskBehavior>>.Unregister(this);
        EventBus<PostBehaviorUpdatedEvent<PersistToDiskBehavior>>.Unregister(this);
        
        // senior-dev: Close LiteDB connection (flushes pending writes)
        _database?.Dispose();
        _database = null;
        _collection = null;
    }

    /// <summary>
    /// Delegate for serializing an entity to JSON.
    /// Set by the application layer (e.g., SceneLoader) to provide entity serialization logic.
    /// </summary>
    public Func<Entity, string>? SerializeEntity { get; set; }

    /// <summary>
    /// Delegate for deserializing and applying JSON to an entity.
    /// Set by the application layer (e.g., SceneLoader) to provide entity deserialization logic.
    /// </summary>
    public Action<Entity, string>? DeserializeEntity { get; set; }

    /// <summary>
    /// Writes all dirty + persistent entities to LiteDB and clears dirty flags.
    /// Called at end-of-frame by game engine or tests.
    /// </summary>
    public void Flush()
    {
        // senior-dev: If database not initialized, skip flush
        if (_collection == null || SerializeEntity == null)
        {
            return;
        }

        try
        {
            // senior-dev: Get all entities with PersistToDiskBehavior
            var persistentEntities = BehaviorRegistry<PersistToDiskBehavior>.Instance.GetActiveBehaviors();
            
            // senior-dev: Clear pre-allocated list for batch write (zero-alloc)
            _documentsToUpsert.Clear();
            
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
                
                // senior-dev: Serialize entity to JSON using caller-provided delegate
                var serializer = SerializeEntity; // Null-forgiving: checked at top of method
                var json = serializer(entity);
                
                // senior-dev: Create LiteDB document (key = PersistToDiskBehavior.Key)
                var doc = new BsonDocument
                {
                    ["_id"] = persistBehavior.Key,
                    ["data"] = json
                };
                
                _documentsToUpsert.Add(doc);
                
                // senior-dev: Clear dirty flag immediately after serialization
                _dirtyFlags.Remove(entity.Index);
            }
            
            // senior-dev: Bulk upsert (per benchmarker findings - use Upsert for updates)
            // LiteDB doesn't have a built-in BulkUpsert, so we do individual upserts
            // This is acceptable per benchmarker findings (simpler than tracking new vs existing)
            foreach (var doc in _documentsToUpsert)
            {
                _collection.Upsert(doc);
            }
            
            // senior-dev: Clear list for next flush (zero-alloc)
            _documentsToUpsert.Clear();
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
    /// Always treats it as a new key - checks DB and loads/writes accordingly.
    /// </summary>
    public void OnEvent(PostBehaviorUpdatedEvent<PersistToDiskBehavior> e)
    {
        // senior-dev: Get new behavior for key
        if (!BehaviorRegistry<PersistToDiskBehavior>.Instance.TryGetBehavior(e.Entity, out var newBehavior))
        {
            return;
        }

        // senior-dev: Validate key
        if (string.IsNullOrWhiteSpace(newBehavior.Value.Key))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"PersistToDiskBehavior key cannot be empty or whitespace for entity {e.Entity.Index}"
            ));
            return;
        }

        // senior-dev: Check if this key exists in database and load/write accordingly
        LoadOrWriteEntity(e.Entity, newBehavior.Value.Key);
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
            
            if (doc != null && DeserializeEntity != null)
            {
                // senior-dev: Key exists in DB - load and apply all behaviors
                var json = doc["data"].AsString;
                
                // senior-dev: Disable dirty tracking during deserialization to prevent unwanted writes
                var wasEnabled = IsEnabled;
                Disable();
                
                DeserializeEntity(entity, json);
                
                if (wasEnabled)
                {
                    Enable();
                }
            }
            else
            {
                // senior-dev: Key doesn't exist in DB - DON'T write immediately
                // Just mark dirty so that Flush() will write the complete entity state
                // This ensures all behaviors have been added before persisting
                MarkDirty(entity.Index);
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
