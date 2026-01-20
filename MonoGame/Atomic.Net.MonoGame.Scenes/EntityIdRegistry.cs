using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Singleton registry for tracking entity IDs and resolving parent references.
/// Maintains bidirectional mappings for O(1) lookups.
/// </summary>
public sealed class EntityIdRegistry : ISingleton<EntityIdRegistry>,
    IEventHandler<InitializeEvent>,
    IEventHandler<BehaviorAddedEvent<IdBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<IdBehavior>>,
    IEventHandler<PreEntityDeactivatedEvent>,
    IEventHandler<ResetEvent>,
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
    private readonly Dictionary<ushort, string> _entityToId = new();

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
    public bool TryResolve(string id, out Entity entity)
    {
        if (_idToEntity.TryGetValue(id, out var foundEntity))
        {
            entity = foundEntity;
            return true;
        }

        entity = default;
        return false;
    }

    public void OnEvent(InitializeEvent _)
    {
        EventBus<BehaviorAddedEvent<IdBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<IdBehavior>>.Register(this);
        EventBus<PreEntityDeactivatedEvent>.Register(this);
        EventBus<ResetEvent>.Register(this);
        EventBus<ShutdownEvent>.Register(this);
    }

    public void OnEvent(BehaviorAddedEvent<IdBehavior> e)
    {
        if (BehaviorRegistry<IdBehavior>.Instance.TryGetBehavior(e.Entity, out var idBehavior))
        {
            TryRegister(e.Entity, idBehavior.Value.Id);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<IdBehavior> e)
    {
        if (BehaviorRegistry<IdBehavior>.Instance.TryGetBehavior(e.Entity, out var idBehavior))
        {
            TryRegister(e.Entity, idBehavior.Value.Id);
        }
    }

    public void OnEvent(PreEntityDeactivatedEvent e)
    {
        if (_entityToId.TryGetValue(e.Entity.Index, out var id))
        {
            _idToEntity.Remove(id);
            _entityToId.Remove(e.Entity.Index);
        }
    }

    public void OnEvent(ResetEvent e)
    {
        var sceneEntitiesToRemove = new List<ushort>();
        
        foreach (var (entityIndex, _) in _entityToId)
        {
            if (entityIndex >= Constants.MaxLoadingEntities)
            {
                sceneEntitiesToRemove.Add(entityIndex);
            }
        }

        foreach (var entityIndex in sceneEntitiesToRemove)
        {
            var id = _entityToId[entityIndex];
            _idToEntity.Remove(id);
            _entityToId.Remove(entityIndex);
        }
    }

    public void OnEvent(ShutdownEvent e)
    {
        _idToEntity.Clear();
        _entityToId.Clear();
    }
}
