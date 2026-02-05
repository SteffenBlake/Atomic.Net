using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using LiteDB;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Persistence;

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
    IEventHandler<PreBehaviorRemovedEvent<PersistToDiskBehavior>>,
    IEventHandler<ShutdownEvent>
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

    private readonly PartitionedSparseArray<bool> _dirtyFlags = new(
        Constants.MaxGlobalEntities,
        Constants.MaxSceneEntities
    );
    private LiteDatabase? _database = null;
    private ILiteCollection<BsonDocument>? _collection = null;
    private readonly PartitionedSparseRefArray<string> _oldKeys = new(
        Constants.MaxGlobalEntities,
        Constants.MaxSceneEntities
    );

    // CRITICAL: Database path is a COMPILATION CONSTANT ONLY
    // NEVER add runtime configuration, environment variables, or any other mechanism to override this
    // The ONLY way to change the path is via compilation constant:
    //   dotnet build -p:DefineConstants=ATOMIC_PERSISTENCE_DB_PATH=\"/custom/path.db\"

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
    public void MarkDirty(PartitionIndex entityIndex)
    {
        // CRITICAL: NEVER bypass this IsEnabled check
        // IsEnabled controls dirty tracking globally - when false, NO entities should be marked dirty
        // This prevents scene construction from triggering disk writes
        if (!IsEnabled)
        {
            return;
        }

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
    /// Database path from Constants.DefaultPersistenceDatabasePath (can be overridden at compile time).
    /// </summary>
    public void OnEvent(InitializeEvent e)
    {
        // Register for EntityMutatedEvent (fired by BehaviorRegistry/RefBehaviorRegistry)
        EventBus<EntityMutatedEvent>.Register(this);

        // Register for PersistToDiskBehavior events (PR comment #2: use overload without explicit handler param - `this` is inferred)
        EventBus<BehaviorAddedEvent<PersistToDiskBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<PersistToDiskBehavior>>.Register(this);
        EventBus<PreBehaviorUpdatedEvent<PersistToDiskBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<PersistToDiskBehavior>>.Register(this);

        // senior-dev: Register for ShutdownEvent to properly close database connection
        EventBus<ShutdownEvent>.Register(this);

        // senior-dev: Only initialize database once to prevent file locking issues
        if (_database != null)
        {
            return;
        }

        // Initialize database with path from Constants (COMPILATION CONSTANT ONLY)
        // NEVER add runtime configuration - path is set at compile time via:
        //   dotnet build -p:DefineConstants=ATOMIC_PERSISTENCE_DB_PATH=\"/custom/path.db\"
        try
        {
            _database = new LiteDatabase(Constants.DefaultPersistenceDatabasePath);
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
        catch (IOException ex)
        {
            // Catch IO exceptions (file locked, access denied, etc.)
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
    /// Handles ShutdownEvent - closes LiteDB connection and cleans up resources.
    /// </summary>
    public void OnEvent(ShutdownEvent e)
    {
        Shutdown();
    }

    /// <summary>
    /// Closes LiteDB connection and cleans up resources.
    /// Called by OnEvent(ShutdownEvent) or can be called manually for testing.
    /// </summary>
    public void Shutdown()
    {
        EventBus<EntityMutatedEvent>.Unregister(this);
        EventBus<BehaviorAddedEvent<PersistToDiskBehavior>>.Unregister(this);
        EventBus<PostBehaviorUpdatedEvent<PersistToDiskBehavior>>.Unregister(this);
        EventBus<PreBehaviorUpdatedEvent<PersistToDiskBehavior>>.Unregister(this);
        EventBus<PreBehaviorRemovedEvent<PersistToDiskBehavior>>.Unregister(this);
        EventBus<ShutdownEvent>.Unregister(this);

        _database?.Dispose();
        _database = null;
        _collection = null;
    }

    private JsonEntity _jsonEntityInstance = new();

    /// <summary>
    /// Writes all dirty + persistent entities to LiteDB and clears dirty flags.
    /// Called at end-of-frame by game engine or tests.
    /// </summary>
    public void Flush()
    {
        // Skip flush if tracking is disabled
        if (!IsEnabled)
        {
            return;
        }

        if (_collection == null)
        {
            throw new InvalidOperationException(
                "DatabaseRegistry.Flush() called before database initialization. Call InitializeDatabase() first."
            );
        }

        var persistentEntities = BehaviorRegistry<PersistToDiskBehavior>.Instance.GetActiveBehaviors();

        foreach (var (entity, persistBehavior) in persistentEntities)
        {
            if (string.IsNullOrWhiteSpace(persistBehavior.Key))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"PersistToDiskBehavior key cannot be empty or whitespace for entity {entity.Index}"
                ));
                continue;
            }

            if (!_dirtyFlags.HasValue(entity.Index))
            {
                continue;
            }

            JsonEntity.ReadFromEntity(entity, ref _jsonEntityInstance);

            var json = System.Text.Json.JsonSerializer.Serialize(_jsonEntityInstance);

            var doc = new BsonDocument
            {
                ["_id"] = persistBehavior.Key,
                ["data"] = json
            };

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

            _dirtyFlags.Remove(entity.Index);
        }
    }

    /// <summary>
    /// Handles PreBehaviorUpdatedEvent for PersistToDiskBehavior to track old keys.
    /// </summary>
    public void OnEvent(PreBehaviorUpdatedEvent<PersistToDiskBehavior> e)
    {
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
        if (!BehaviorRegistry<PersistToDiskBehavior>.Instance.TryGetBehavior(e.Entity, out var behavior))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(behavior.Value.Key))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"PersistToDiskBehavior key cannot be empty or whitespace for entity {e.Entity.Index}"
            ));
            return;
        }

        LoadOrMarkDirty(e.Entity, behavior.Value.Key);
    }

    /// <summary>
    /// Handles PersistToDiskBehavior key changes (key swaps).
    /// Compares oldKey vs newKey to prevent infinite loops.
    /// </summary>
    public void OnEvent(PostBehaviorUpdatedEvent<PersistToDiskBehavior> e)
    {
        if (!BehaviorRegistry<PersistToDiskBehavior>.Instance.TryGetBehavior(e.Entity, out var newBehavior))
        {
            return;
        }

        var oldKey = _oldKeys.TryGetValue(e.Entity.Index, out var key) ? key : null;
        var newKey = newBehavior.Value.Key;

        // Infinite loop prevention
        if (oldKey == newKey)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(newKey))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"PersistToDiskBehavior key cannot be empty or whitespace for entity {e.Entity.Index}"
            ));
            return;
        }

        LoadOrMarkDirty(e.Entity, newKey);
    }

    /// <summary>
    /// Handles PersistToDiskBehavior being removed from an entity.
    /// Writes final state to disk if entity is dirty.
    /// </summary>
    public void OnEvent(PreBehaviorRemovedEvent<PersistToDiskBehavior> e)
    {
        if (!BehaviorRegistry<PersistToDiskBehavior>.Instance.TryGetBehavior(e.Entity, out var behavior))
        {
            return;
        }

        if (_collection == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(behavior.Value.Key))
        {
            return;
        }

        // SteffenBlake: This is the correct behavior, removing the behavior should just
        // Stop the entity from getting tracked, full stop
        // If tests are asserting otherwise, those tests are wrong
        _dirtyFlags.Remove(e.Entity.Index);
    }

    private void LoadOrMarkDirty(Entity entity, string key)
    {
        if (!TryLoadFromDatabase(key, out var json))
        {
            // CRITICAL: NEVER bypass IsEnabled check - 
            // always use MarkDirty() which respects IsEnabled
            MarkDirty(entity.Index);
            return;
        }

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
        }

        if (wasEnabled)
        {
            Enable();
        }
    }

    private bool TryLoadFromDatabase(
        string key,
        [NotNullWhen(true)]
        out string? json
    )
    {
        if (!TryGetDoc(key, out var doc))
        {
            json = null;
            return false;
        }

        json = doc["data"].AsString;
        return true;
    }

    private bool TryGetDoc(
        string key,
        [NotNullWhen(true)]
        out BsonDocument? doc
    )
    {
        try
        {
            doc = _collection?.FindById(key);
        }
        catch (LiteException ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to find entity with key '{key}': {ex.Message}"
            ));
            doc = null;
        }

        return doc != null;
    }

    /// <summary>
    /// Resets scene partition. Database records are automatically cleaned up through
    /// behavior removal events when entities are deactivated by EntityRegistry.
    /// Called by ResetDriver during scene transitions.
    /// </summary>
    public void Reset()
    {
        // No-op: Database records are cleaned up automatically via PreBehaviorRemovedEvent
        // when scene entities are deactivated by EntityRegistry.Reset()
    }
}
