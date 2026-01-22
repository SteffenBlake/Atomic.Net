using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using LiteDB;

namespace Atomic.Net.MonoGame.Scenes.Persistence;

/// <summary>
/// Central persistence system that manages dirty tracking and LiteDB integration.
/// Tracks entity mutations via EntityMutatedEvent for zero-allocation flagging.
/// </summary>
public sealed class DatabaseRegistry : ISingleton<DatabaseRegistry>,
    IEventHandler<InitializeEvent>,
    IEventHandler<EntityMutatedEvent>,
    IEventHandler<BehaviorAddedEvent<PersistToDiskBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<PersistToDiskBehavior>>,
    IEventHandler<PreBehaviorUpdatedEvent<PersistToDiskBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<PersistToDiskBehavior>>
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
    /// Handles InitializeEvent - registers for behavior events and initializes database.
    /// Database path from environment variable ATOMIC_PERSISTENCE_DB_PATH or default.
    /// </summary>
    public void OnEvent(InitializeEvent e)
    {
        // senior-dev: Register for EntityMutatedEvent (fired by BehaviorRegistry/RefBehaviorRegistry)
        EventBus<EntityMutatedEvent>.Register(this);
        
        // senior-dev: Register for PersistToDiskBehavior events (PR comment #2: use overload without explicit handler param - `this` is inferred)
        EventBus<BehaviorAddedEvent<PersistToDiskBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<PersistToDiskBehavior>>.Register(this);
        EventBus<PreBehaviorUpdatedEvent<PersistToDiskBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<PersistToDiskBehavior>>.Register(this);
        
        // senior-dev: Initialize database with path from environment variable or default (PR comment #5)
        // Tests specify path via: dotnet test -e ATOMIC_PERSISTENCE_DB_PATH=/path/to/db
        var dbPath = Environment.GetEnvironmentVariable("ATOMIC_PERSISTENCE_DB_PATH") 
                     ?? Constants.DefaultPersistenceDatabasePath;
        
        try
        {
            _database = new LiteDatabase(dbPath);
            _collection = _database.GetCollection<BsonDocument>("entities");
            _collection.EnsureIndex("_id");
        }
        catch (LiteException ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to initialize persistence database: {ex.Message}"
            ));
            // Leave _database and _collection as null (graceful degradation)
        }
    }
    
    /// <summary>
    /// Handles EntityMutatedEvent - marks entity as dirty for persistence.
    /// </summary>
    public void OnEvent(EntityMutatedEvent e)
    {
        MarkDirty(e.Entity.Index);
    }

    /// <summary>
    /// Closes LiteDB connection and cleans up resources.
    /// </summary>
    public void Shutdown()
    {
        EventBus<EntityMutatedEvent>.Unregister(this);
        EventBus<BehaviorAddedEvent<PersistToDiskBehavior>>.Unregister(this);
        EventBus<PostBehaviorUpdatedEvent<PersistToDiskBehavior>>.Unregister(this);
        EventBus<PreBehaviorUpdatedEvent<PersistToDiskBehavior>>.Unregister(this);
        EventBus<PreBehaviorRemovedEvent<PersistToDiskBehavior>>.Unregister(this);
        
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
        // senior-dev: Skip flush if tracking is disabled
        if (!IsEnabled)
        {
            return;
        }
        
        // senior-dev: Throw if database not initialized (per PR requirements)
        if (_collection == null)
        {
            throw new InvalidOperationException(
                "DatabaseRegistry.Flush() called before database initialization. Call InitializeDatabase() first."
            );
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

    /// <summary>
    /// Handles PersistToDiskBehavior being removed from an entity.
    /// Writes final state to disk if entity is dirty.
    /// </summary>
    public void OnEvent(PreBehaviorRemovedEvent<PersistToDiskBehavior> e)
    {
        // senior-dev: Get the behavior before it's removed
        if (!BehaviorRegistry<PersistToDiskBehavior>.Instance.TryGetBehavior(e.Entity, out var behavior))
        {
            return;
        }

        // senior-dev: Skip if database not initialized
        if (_collection == null)
        {
            return;
        }

        // senior-dev: Validate key
        if (string.IsNullOrWhiteSpace(behavior.Value.Key))
        {
            return;
        }

        // senior-dev: Write final state to disk if entity is dirty
        // This ensures the last changes are persisted before the behavior is removed
        if (_dirtyFlags.HasValue(e.Entity.Index))
        {
            var json = SceneLoader.SerializeEntityToJson(e.Entity);
            var doc = new BsonDocument
            {
                ["_id"] = behavior.Value.Key,
                ["data"] = json
            };

            try
            {
                _collection.Upsert(doc);
                _dirtyFlags.Remove(e.Entity.Index);
            }
            catch (LiteException ex)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Failed to write final state for entity {e.Entity.Index} before behavior removal: {ex.Message}"
                ));
            }
        }
    }

    // senior-dev: Helper method to load entity from DB or mark dirty
    // Uses TryPattern per AGENTS.md guidelines (PR comment #8)
    private void LoadOrMarkDirty(Entity entity, string key)
    {
        if (!TryLoadFromDatabase(key, out var json))
        {
            // senior-dev: Key doesn't exist in DB - mark dirty for first write
            MarkDirty(entity.Index);
            return;
        }
        
        // senior-dev: Key exists in DB - load and apply all behaviors
        // Disable dirty tracking during deserialization to prevent unwanted writes
        var wasEnabled = IsEnabled;
        Disable();
        
        try
        {
            SceneLoader.DeserializeEntityFromJson(entity, json);
        }
        catch (JsonException ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to deserialize entity with key '{key}': {ex.Message}"
            ));
            // Entity keeps current state (graceful degradation)
        }
        
        if (wasEnabled)
        {
            Enable();
        }
    }
    
    // senior-dev: TryPattern for database loading (per AGENTS.md - PR comment #8)
    private bool TryLoadFromDatabase(
        string key,
        [NotNullWhen(true)] out string? json)
    {
        json = null;
        
        if (_collection == null)
        {
            return false;
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
            return false;
        }
        
        if (doc == null)
        {
            return false;
        }
        
        json = doc["data"].AsString;
        return true;
    }
}
