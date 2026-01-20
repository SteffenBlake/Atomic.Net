using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED;

/// <summary>
/// Singleton registry for tracking entity IDs and resolving parent references.
/// </summary>
public sealed class EntityIdRegistry : ISingleton<EntityIdRegistry>,
    IEventHandler<InitializeEvent>,
    IEventHandler<BehaviorAddedEvent<IdBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<IdBehavior>>,
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

    // senior-dev: Pre-sized dictionaries to avoid reallocations during scene loading
    private readonly Dictionary<string, Entity> _idToEntity = new(Constants.MaxEntities);
    private readonly Dictionary<ushort, string> _entityToId = new(Constants.MaxEntities);
    
    // senior-dev: Pre-allocated array for ResetEvent to avoid List allocations
    private readonly ushort[] _entitiesToRemove = new ushort[Constants.MaxEntities];

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
        return _idToEntity.TryGetValue(id, out entity);
    }

    public void OnEvent(InitializeEvent _)
    {
        EventBus<BehaviorAddedEvent<IdBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<IdBehavior>>.Register(this);
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

    // senior-dev: Clean up ID mapping when IdBehavior is removed (entity deactivation or explicit removal)
    public void OnEvent(PreBehaviorRemovedEvent<IdBehavior> e)
    {
        if (_entityToId.TryGetValue(e.Entity.Index, out var id))
        {
            _idToEntity.Remove(id);
            _entityToId.Remove(e.Entity.Index);
        }
    }

    public void OnEvent(ResetEvent e)
    {
        // senior-dev: Use pre-allocated array to avoid List allocation
        var removeCount = 0;
        
        foreach (var (entityIndex, _) in _entityToId)
        {
            if (entityIndex >= Constants.MaxLoadingEntities)
            {
                _entitiesToRemove[removeCount++] = entityIndex;
            }
        }

        for (var i = 0; i < removeCount; i++)
        {
            var entityIndex = _entitiesToRemove[i];
            var id = _entityToId[entityIndex];
            _idToEntity.Remove(id);
            _entityToId.Remove(entityIndex);
        }
    }

    // senior-dev: Clean up all mappings on shutdown
    public void OnEvent(ShutdownEvent e)
    {
        _idToEntity.Clear();
        _entityToId.Clear();
    }
}
