using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Ids;

/// <summary>
/// Singleton registry for tracking entity IDs and resolving parent references.
/// Thread-safe using ConcurrentDictionary for async scene loading.
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

    // Thread-safe for async scene loading
    private readonly ConcurrentDictionary<string, Entity> _idToEntity = new();
    private readonly PartitionedSparseRefArray<string> _entityToId = new(
        Constants.MaxGlobalEntities,
        Constants.MaxSceneEntities
    );

    /// <summary>
    /// Attempts to register an entity with the given ID.
    /// Returns true if registered successfully (first-write-wins).
    /// Returns false if the ID is already registered.
    /// Thread-safe for concurrent calls.
    /// </summary>
    public bool TryRegister(Entity entity, string id)
    {
        if (!_idToEntity.TryAdd(id, entity))
        {
            return false;
        }

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
            _idToEntity.TryRemove(id, out _);
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
            _idToEntity.TryRemove(id, out _);
            _entityToId.Remove(e.Entity.Index);
        }
    }

    /// <summary>
    /// Resets scene partition by clearing only scene entity IDs.
    /// Called by ResetDriver during scene transitions.
    /// </summary>
    public void Reset()
    {
        // Clear IDs for scene entities (thread-safe TryRemove)
        foreach (var (index, id) in _entityToId.Scene)
        {
            _idToEntity.TryRemove(id, out _);
        }

        // Clear the scene partition
        _entityToId.Scene.Clear();
    }
}
