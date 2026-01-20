using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Singleton registry for tracking entity IDs and resolving parent references.
/// Maintains a dictionary mapping string IDs to entities.
/// </summary>
public sealed class EntityIdRegistry : ISingleton<EntityIdRegistry>,
    IEventHandler<BehaviorAddedEvent<EntityId>>,
    IEventHandler<PostBehaviorUpdatedEvent<EntityId>>,
    IEventHandler<PreEntityDeactivatedEvent>,
    IEventHandler<ResetEvent>,
    IEventHandler<ShutdownEvent>
{
    private static EntityIdRegistry? _instance;
    public static EntityIdRegistry Instance => _instance ??= new EntityIdRegistry();

    private readonly Dictionary<string, Entity> _idToEntity = new();

    private EntityIdRegistry()
    {
        EventBus<BehaviorAddedEvent<EntityId>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<EntityId>>.Register(this);
        EventBus<PreEntityDeactivatedEvent>.Register(this);
        EventBus<ResetEvent>.Register(this);
        EventBus<ShutdownEvent>.Register(this);
    }

    /// <summary>
    /// Attempts to register an entity with the given ID.
    /// Returns true if registered successfully (first-write-wins).
    /// Returns false if the ID is already registered.
    /// </summary>
    public bool TryRegister(Entity entity, string id)
    {
        // test-architect: Stub - To be implemented by #senior-dev
        // senior-dev: First-write-wins - if ID already exists, return false
        if (_idToEntity.ContainsKey(id))
        {
            return false;
        }

        _idToEntity[id] = entity;
        return true;
    }

    /// <summary>
    /// Attempts to resolve an entity by its ID.
    /// Returns true and outputs the entity if found, false otherwise.
    /// </summary>
    public bool TryResolve(string id, out Entity entity)
    {
        // test-architect: Stub - To be implemented by #senior-dev
        // senior-dev: Simple dictionary lookup
        return _idToEntity.TryGetValue(id, out entity);
    }

    public void OnEvent(BehaviorAddedEvent<EntityId> e)
    {
        // test-architect: Stub - To be implemented by #senior-dev
        // senior-dev: When EntityId behavior is added, register it in the lookup
        // senior-dev: Need to get the actual EntityId behavior to read the ID string
        if (BehaviorRegistry<EntityId>.Instance.TryGetBehavior(e.Entity, out var entityId))
        {
            TryRegister(e.Entity, entityId.Value.Id);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<EntityId> e)
    {
        // senior-dev: FINDING: BehaviorRegistry<EntityId> is initialized AFTER InitializeEvent fires
        // senior-dev: This means it never registers for PreEntityDeactivatedEvent in OnEvent(InitializeEvent)
        // senior-dev: So when ResetEvent deactivates entities, EntityId behaviors are NOT removed
        // senior-dev: On second scene load, SetBehavior sees existing behavior and fires UpdatedEvent instead of AddedEvent
        // senior-dev: Solution: Also listen to PostBehaviorUpdatedEvent to handle this case
        // senior-dev: This ensures entity IDs are registered even when behavior is updated instead of added
        if (BehaviorRegistry<EntityId>.Instance.TryGetBehavior(e.Entity, out var entityId))
        {
            TryRegister(e.Entity, entityId.Value.Id);
        }
    }

    public void OnEvent(PreEntityDeactivatedEvent e)
    {
        // test-architect: Stub - To be implemented by #senior-dev
        // senior-dev: When entity is deactivated, remove it from the ID lookup
        // senior-dev: Need to find and remove the ID mapping for this entity
        // senior-dev: Using LINQ here is acceptable since this is not a hot path (only during entity deactivation)
        var idToRemove = _idToEntity.FirstOrDefault(kvp => kvp.Value.Index == e.Entity.Index).Key;
        if (idToRemove != null)
        {
            _idToEntity.Remove(idToRemove);
        }
    }

    public void OnEvent(ResetEvent e)
    {
        // test-architect: Stub - To be implemented by #senior-dev
        // senior-dev: Clear all scene entity IDs (scene entities have index >= MaxLoadingEntities)
        // senior-dev: Keep loading entity IDs intact
        var sceneIdsToRemove = _idToEntity
            .Where(kvp => kvp.Value.Index >= Constants.MaxLoadingEntities)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in sceneIdsToRemove)
        {
            _idToEntity.Remove(id);
        }
    }

    public void OnEvent(ShutdownEvent e)
    {
        // senior-dev: Clear ALL entity IDs (both loading and scene partitions)
        // senior-dev: Used for complete game shutdown and test cleanup
        _idToEntity.Clear();
    }
}
