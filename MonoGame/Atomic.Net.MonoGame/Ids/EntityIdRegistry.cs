using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Ids;

/// <summary>
/// Singleton registry for tracking entity IDs and resolving parent references.
/// </summary>
public sealed class EntityIdRegistry : ISingleton<EntityIdRegistry>,
    IEventHandler<InitializeEvent>,
    IEventHandler<BehaviorAddedEvent<IdBehavior>>,
    IEventHandler<PreBehaviorUpdatedEvent<IdBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<IdBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<IdBehavior>>,
    IEventHandler<ShutdownEvent>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
        EventBus<InitializeEvent>.Register(Instance);
    }

    public static EntityIdRegistry Instance { get; private set; } = null!;

    private readonly Dictionary<string, Entity> _idToEntity = new();
    private readonly PartitionedSparseRefArray<string> _entityToId = new(
        Constants.MaxGlobalEntities,
        Constants.MaxSceneEntities
    );

    /// <summary>
    /// Attempts to register an entity with the given ID.
    /// Returns true if registered successfully (first-write-wins).
    /// Returns false if the ID is already registered.
    /// </summary>
    public bool TryRegister(Entity entity, string id)
    {
        if (_idToEntity.ContainsKey(id))
        {
            return false;
        }

        _idToEntity[id] = entity;
        _entityToId[entity.Index] = id;
        return true;
    }

    /// <summary>
    /// Attempts to resolve an entity by its ID.
    /// Returns true and outputs the entity if found, false otherwise.
    /// </summary>
    public bool TryResolve(
        string id, 
        [NotNullWhen(true)]
        out Entity? entity
    )
    {
        // Dictionary.TryGetValue doesnt handle nullability of value types
        if (_idToEntity.TryGetValue(id, out var located))
        {
            entity = located;
            return true;
        }

        entity = null;
        return false;
    }

    public void OnEvent(InitializeEvent _)
    {
        EventBus<BehaviorAddedEvent<IdBehavior>>.Register(this);
        EventBus<PreBehaviorUpdatedEvent<IdBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<IdBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<IdBehavior>>.Register(this);
        EventBus<ShutdownEvent>.Register(this);
    }

    public void OnEvent(ShutdownEvent _)
    {
        // senior-dev: Unregister from all events to prevent duplicate registrations
        // when InitializeEvent is pushed again in subsequent tests
        EventBus<BehaviorAddedEvent<IdBehavior>>.Unregister(this);
        EventBus<PreBehaviorUpdatedEvent<IdBehavior>>.Unregister(this);
        EventBus<PostBehaviorUpdatedEvent<IdBehavior>>.Unregister(this);
        EventBus<PreBehaviorRemovedEvent<IdBehavior>>.Unregister(this);
        EventBus<ShutdownEvent>.Unregister(this);
    }

    public void OnEvent(BehaviorAddedEvent<IdBehavior> e)
    {
        if (BehaviorRegistry<IdBehavior>.Instance.TryGetBehavior(e.Entity, out var idBehavior))
        {
            TryRegister(e.Entity, idBehavior.Value.Id);
        }
    }

    public void OnEvent(PreBehaviorUpdatedEvent<IdBehavior> e)
    {
        if (_entityToId.TryGetValue(e.Entity.Index, out var id))
        {
            _idToEntity.Remove(id);
            _entityToId.Remove(e.Entity.Index);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<IdBehavior> e)
    {
        if (BehaviorRegistry<IdBehavior>.Instance.TryGetBehavior(e.Entity, out var idBehavior))
        {
            TryRegister(e.Entity, idBehavior.Value.Id);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<IdBehavior> e)
    {
        if (_entityToId.TryGetValue(e.Entity.Index, out var id))
        {
            _idToEntity.Remove(id);
            _entityToId.Remove(e.Entity.Index);
        }
    }
}
