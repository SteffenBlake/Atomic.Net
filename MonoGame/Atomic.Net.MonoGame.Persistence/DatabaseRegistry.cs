using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;

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
        // test-architect: Stub implementation - actual dirty tracking will use SparseArray<bool>
        if (!IsEnabled) return;
        // TODO: Set _dirtyFlags[entityIndex] = true when SparseArray is implemented
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
        // test-architect: Stub implementation - actual flush will write to LiteDB
        if (!IsEnabled) return;
        // TODO: Implement dirty+persistent entity serialization and LiteDB write
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
    public void Initialize()
    {
        // test-architect: To be implemented by @senior-dev
        // Stub: No-op for now - actual LiteDB connection will be created here
    }

    /// <summary>
    /// Closes LiteDB connection and cleans up resources.
    /// Called once at application shutdown.
    /// </summary>
    public void Shutdown()
    {
        // test-architect: To be implemented by @senior-dev
        // Stub: No-op for now - actual LiteDB connection will be closed here
    }

    /// <summary>
    /// Handles PersistToDiskBehavior being added to an entity.
    /// Checks DB for existing document with behavior.Key:
    /// - If exists: Load all behaviors from DB and apply to entity
    /// - If not exists: Serialize current entity state and write to DB
    /// </summary>
    public void OnEvent(BehaviorAddedEvent<PersistToDiskBehavior> e)
    {
        // test-architect: Stub implementation - actual implementation will check LiteDB and load/save
        // TODO: Check if DB has document with e.Behavior.Key, load if exists, write if not
    }

    /// <summary>
    /// Handles PersistToDiskBehavior key changes (key swaps).
    /// Compares oldBehavior.Key vs newBehavior.Key:
    /// - If oldKey == newKey: Skip (infinite loop prevention when loading from disk)
    /// - If oldKey != newKey: Check DB for newKey, load if exists, write if not
    /// </summary>
    public void OnEvent(PostBehaviorUpdatedEvent<PersistToDiskBehavior> e)
    {
        // test-architect: Stub implementation - actual implementation will handle key swaps
        // TODO: Compare e.OldBehavior.Key vs e.NewBehavior.Key, handle key swap logic
    }
}
