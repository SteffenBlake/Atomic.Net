using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using LiteDB;

namespace Atomic.Net.MonoGame.Scenes.Persistence;

/// <summary>
/// Central persistence system that manages dirty tracking and LiteDB integration.
/// Tracks entity mutations via EventBus for zero-allocation flagging.
/// </summary>
public sealed class DatabaseRegistry : ISingleton<DatabaseRegistry>,
    IEventHandler<InitializeEvent>,
    IEventHandler<BehaviorAddedEvent<PersistToDiskBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<PersistToDiskBehavior>>,
    IEventHandler<PreBehaviorUpdatedEvent<PersistToDiskBehavior>>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = new();
        EventBus<InitializeEvent>.Register(Instance);
    }

    public static DatabaseRegistry Instance { get; private set; } = null!;
    
    private DatabaseRegistry() { }

    // senior-dev: Dirty tracking sparse array (zero-alloc flagging)
    private readonly SparseArray<bool> _dirtyFlags = new(Constants.MaxEntities);
    
    // senior-dev: LiteDB connection (null until InitializeDatabase() is called)
    private LiteDatabase? _database = null;
    private ILiteCollection<BsonDocument>? _collection = null;
    
    // senior-dev: Track old keys for infinite loop prevention
    private readonly SparseReferenceArray<string> _oldKeys = new(Constants.MaxEntities);

    /// <summary>
    /// Gets whether dirty tracking is currently enabled.
    /// Disabled during scene load to prevent unwanted disk writes.
    /// </summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>
    /// Marks an entity as dirty (has mutations that need to be persisted).
    /// Called via EventBus on behavior mutations.
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
    /// Handles InitializeEvent - registers for behavior events.
    /// </summary>
    public void OnEvent(InitializeEvent e)
    {
        // senior-dev: Register for PersistToDiskBehavior events
        EventBus<BehaviorAddedEvent<PersistToDiskBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<PersistToDiskBehavior>>.Register(this);
        EventBus<PreBehaviorUpdatedEvent<PersistToDiskBehavior>>.Register(this);
        
        // senior-dev: Register for ALL behavior mutation events to track dirty entities
        // Replaces the forbidden BehaviorMutationTracker pattern with proper EventBus usage
        var handler = new BehaviorUpdateHandler(this);
        EventBus<PostBehaviorUpdatedEvent<BED.IdBehavior>>.Register<IEventHandler<PostBehaviorUpdatedEvent<BED.IdBehavior>>>(handler);
        EventBus<PostBehaviorUpdatedEvent<BED.Properties.PropertiesBehavior>>.Register<IEventHandler<PostBehaviorUpdatedEvent<BED.Properties.PropertiesBehavior>>>(handler);
        EventBus<PostBehaviorUpdatedEvent<Transform.TransformBehavior>>.Register<IEventHandler<PostBehaviorUpdatedEvent<Transform.TransformBehavior>>>(handler);
        EventBus<PostBehaviorUpdatedEvent<BED.Hierarchy.Parent>>.Register<IEventHandler<PostBehaviorUpdatedEvent<BED.Hierarchy.Parent>>>(handler);
        
        EventBus<BehaviorAddedEvent<BED.IdBehavior>>.Register<IEventHandler<BehaviorAddedEvent<BED.IdBehavior>>>(handler);
        EventBus<BehaviorAddedEvent<BED.Properties.PropertiesBehavior>>.Register<IEventHandler<BehaviorAddedEvent<BED.Properties.PropertiesBehavior>>>(handler);
        EventBus<BehaviorAddedEvent<Transform.TransformBehavior>>.Register<IEventHandler<BehaviorAddedEvent<Transform.TransformBehavior>>>(handler);
        EventBus<BehaviorAddedEvent<BED.Hierarchy.Parent>>.Register<IEventHandler<BehaviorAddedEvent<BED.Hierarchy.Parent>>>(handler);
    }

    // senior-dev: Helper class to handle generic behavior updates and mark entities dirty
    // Replaces the forbidden BehaviorMutationTracker pattern with proper EventBus usage
    private sealed class BehaviorUpdateHandler :
        IEventHandler<PostBehaviorUpdatedEvent<BED.IdBehavior>>,
        IEventHandler<PostBehaviorUpdatedEvent<BED.Properties.PropertiesBehavior>>,
        IEventHandler<PostBehaviorUpdatedEvent<Transform.TransformBehavior>>,
        IEventHandler<PostBehaviorUpdatedEvent<BED.Hierarchy.Parent>>,
        IEventHandler<BehaviorAddedEvent<BED.IdBehavior>>,
        IEventHandler<BehaviorAddedEvent<BED.Properties.PropertiesBehavior>>,
        IEventHandler<BehaviorAddedEvent<Transform.TransformBehavior>>,
        IEventHandler<BehaviorAddedEvent<BED.Hierarchy.Parent>>
    {
        private readonly DatabaseRegistry _registry;
        
        public BehaviorUpdateHandler(DatabaseRegistry registry)
        {
            _registry = registry;
        }
        
        public void OnEvent(PostBehaviorUpdatedEvent<BED.IdBehavior> e) => _registry.MarkDirty(e.Entity.Index);
        public void OnEvent(PostBehaviorUpdatedEvent<BED.Properties.PropertiesBehavior> e) => _registry.MarkDirty(e.Entity.Index);
        public void OnEvent(PostBehaviorUpdatedEvent<Transform.TransformBehavior> e) => _registry.MarkDirty(e.Entity.Index);
        public void OnEvent(PostBehaviorUpdatedEvent<BED.Hierarchy.Parent> e) => _registry.MarkDirty(e.Entity.Index);
        public void OnEvent(BehaviorAddedEvent<BED.IdBehavior> e) => _registry.MarkDirty(e.Entity.Index);
        public void OnEvent(BehaviorAddedEvent<BED.Properties.PropertiesBehavior> e) => _registry.MarkDirty(e.Entity.Index);
        public void OnEvent(BehaviorAddedEvent<Transform.TransformBehavior> e) => _registry.MarkDirty(e.Entity.Index);
        public void OnEvent(BehaviorAddedEvent<BED.Hierarchy.Parent> e) => _registry.MarkDirty(e.Entity.Index);
    }

    /// <summary>
    /// Initializes LiteDB connection.
    /// Called explicitly by tests or application code.
    /// </summary>
    public void InitializeDatabase(string dbPath = "persistence.db")
    {
        _database = new LiteDatabase(dbPath);
        _collection = _database.GetCollection<BsonDocument>("entities");
        _collection.EnsureIndex("_id");
    }

    /// <summary>
    /// Closes LiteDB connection and cleans up resources.
    /// </summary>
    public void Shutdown()
    {
        EventBus<BehaviorAddedEvent<PersistToDiskBehavior>>.Unregister(this);
        EventBus<PostBehaviorUpdatedEvent<PersistToDiskBehavior>>.Unregister(this);
        EventBus<PreBehaviorUpdatedEvent<PersistToDiskBehavior>>.Unregister(this);
        
        _database?.Dispose();
        _database = null;
        _collection = null;
    }

    /// <summary>
    /// Writes all dirty + persistent entities to LiteDB and clears dirty flags.
    /// Called at end-of-frame by game engine or tests.
    /// </summary>
    public void Flush()
    {
        // senior-dev: If database not initialized, skip flush
        if (_collection == null)
        {
            return;
        }

        // senior-dev: Get all entities with PersistToDiskBehavior
        var persistentEntities = BehaviorRegistry<PersistToDiskBehavior>.Instance.GetActiveBehaviors();
        
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
            
            // senior-dev: Serialize entity to JSON
            var json = SceneLoader.SerializeEntityToJson(entity);
            
            // senior-dev: Create LiteDB document (key = PersistToDiskBehavior.Key)
            var doc = new BsonDocument
            {
                ["_id"] = persistBehavior.Key,
                ["data"] = json
            };
            
            // senior-dev: Write directly to DB - only wrap the ONE operation that can throw
            try
            {
                _collection.Upsert(doc);
            }
            catch (LiteException ex)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Failed to upsert entity {entity.Index} with key '{persistBehavior.Key}': {ex.Message}"
                ));
                continue;
            }
            
            // senior-dev: Clear dirty flag immediately after successful write
            _dirtyFlags.Remove(entity.Index);
        }
    }

    /// <summary>
    /// Handles PreBehaviorUpdatedEvent for PersistToDiskBehavior to track old keys.
    /// </summary>
    public void OnEvent(PreBehaviorUpdatedEvent<PersistToDiskBehavior> e)
    {
        // senior-dev: Store old key for infinite loop prevention
        if (BehaviorRegistry<PersistToDiskBehavior>.Instance.TryGetBehavior(e.Entity, out var oldBehavior))
        {
            _oldKeys[e.Entity.Index] = oldBehavior.Value.Key;
        }
    }

    /// <summary>
    /// Handles PersistToDiskBehavior being added to an entity.
    /// Checks DB for existing document with behavior.Key:
    /// - If exists: Load all behaviors from DB and apply to entity
    /// - If not exists: Mark dirty for first write
    /// </summary>
    public void OnEvent(BehaviorAddedEvent<PersistToDiskBehavior> e)
    {
        // senior-dev: Get the behavior that was just added
        if (!BehaviorRegistry<PersistToDiskBehavior>.Instance.TryGetBehavior(e.Entity, out var behavior))
        {
            return;
        }

        // senior-dev: Validate key (fire ErrorEvent for empty/null/whitespace keys)
        if (string.IsNullOrWhiteSpace(behavior.Value.Key))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"PersistToDiskBehavior key cannot be empty or whitespace for entity {e.Entity.Index}"
            ));
            return;
        }

        // senior-dev: Load from DB if exists, mark dirty if not
        LoadOrMarkDirty(e.Entity, behavior.Value.Key);
    }

    /// <summary>
    /// Handles PersistToDiskBehavior key changes (key swaps).
    /// Compares oldKey vs newKey to prevent infinite loops.
    /// </summary>
    public void OnEvent(PostBehaviorUpdatedEvent<PersistToDiskBehavior> e)
    {
        // senior-dev: Get new behavior for key
        if (!BehaviorRegistry<PersistToDiskBehavior>.Instance.TryGetBehavior(e.Entity, out var newBehavior))
        {
            return;
        }

        // senior-dev: Get old key (stored in PreBehaviorUpdatedEvent)
        var oldKey = _oldKeys.TryGetValue(e.Entity.Index, out var key) ? key : null;
        var newKey = newBehavior.Value.Key;

        // senior-dev: Infinite loop prevention - if keys match, skip DB operation
        if (oldKey == newKey)
        {
            return;
        }

        // senior-dev: Validate new key (fire ErrorEvent for empty/null/whitespace keys)
        if (string.IsNullOrWhiteSpace(newKey))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"PersistToDiskBehavior key cannot be empty or whitespace for entity {e.Entity.Index}"
            ));
            return;
        }

        // senior-dev: Key changed - check DB and load/mark dirty
        LoadOrMarkDirty(e.Entity, newKey);
    }

    // senior-dev: Helper method to load entity from DB or mark dirty
    private void LoadOrMarkDirty(Entity entity, string key)
    {
        if (_collection == null)
        {
            return;
        }

        BsonDocument? doc = null;
        try
        {
            doc = _collection.FindById(key);
        }
        catch (LiteException ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to find entity with key '{key}': {ex.Message}"
            ));
            return;
        }
        
        if (doc != null)
        {
            // senior-dev: Key exists in DB - load and apply all behaviors
            var json = doc["data"].AsString;
            
            // senior-dev: Disable dirty tracking during deserialization to prevent unwanted writes
            var wasEnabled = IsEnabled;
            Disable();
            
            SceneLoader.DeserializeEntityFromJson(entity, json);
            
            if (wasEnabled)
            {
                Enable();
            }
        }
        else
        {
            // senior-dev: Key doesn't exist in DB - mark dirty for first write
            MarkDirty(entity.Index);
        }
    }
}
