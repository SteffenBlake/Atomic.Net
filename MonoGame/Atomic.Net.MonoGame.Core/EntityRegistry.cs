
namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Central registry for entity lifecycle management.
/// </summary>
public class EntityRegistry : IEventHandler<ResetEvent>, IEventHandler<ShutdownEvent>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
        EventBus<ResetEvent>.Register(Instance);
        EventBus<ShutdownEvent>.Register(Instance);
    }

    public static EntityRegistry Instance { get; private set; } = null!;

    private readonly Entity[] _entities = [.. 
        Enumerable.Range(0, Constants.MaxEntities - 1)
        .Select(index => new Entity((ushort)index))
    ];

    private readonly SparseArray<bool> _active = new(Constants.MaxEntities);
    private readonly SparseArray<bool> _enabled = new(Constants.MaxEntities);
    private ushort _nextSceneIndex = Constants.MaxPersistentEntities;
    private ushort _nextPersistentIndex = 0;

    public Entity this[ushort index]
    {
        get
        {
            return _entities[index];
        }
    }

    /// <summary>
    /// Activate the next available scene entity (index greater than or equal to MaxPersistentEntities).
    /// </summary>
    /// <returns>The activated entity.</returns>
    public Entity Activate()
    {
        for (ushort offset = 0; offset < Constants.MaxEntities - Constants.MaxPersistentEntities; offset++)
        {
            ushort i = GetNextSceneIndex(offset);
            
            if (_active[i])
            {
                continue;
            }

            _active.Set(i, true);
            _enabled.Set(i, true);
            _nextSceneIndex = (ushort)(i + 1);
            if (_nextSceneIndex >= Constants.MaxEntities)
            {
                _nextSceneIndex = Constants.MaxPersistentEntities;
            }

            return new(i);
        }

        throw new InvalidOperationException("MaxEntities (scene partition) reached");
    }

    /// <summary>
    /// Activate the next available persistent entity (index less than MaxPersistentEntities).
    /// </summary>
    /// <returns>The activated entity.</returns>
    public Entity ActivatePersistent()
    {
        for (ushort offset = 0; offset < Constants.MaxPersistentEntities; offset++)
        {
            ushort i = (ushort)((_nextPersistentIndex + offset) % Constants.MaxPersistentEntities);
            
            if (_active[i])
            {
                continue;
            }

            _active.Set(i, true);
            _enabled.Set(i, true);
            _nextPersistentIndex = (ushort)((i + 1) % Constants.MaxPersistentEntities);

            return new(i);
        }

        throw new InvalidOperationException("MaxPersistentEntities reached");
    }

    private ushort GetNextSceneIndex(ushort offset)
    {
        return (ushort)(Constants.MaxPersistentEntities + 
            ((_nextSceneIndex - Constants.MaxPersistentEntities + offset) % 
            (Constants.MaxEntities - Constants.MaxPersistentEntities)));
    }

    /// <summary>
    /// Get the first scene entity (index MaxPersistentEntities).
    /// </summary>
    /// <returns>The scene root entity.</returns>
    public Entity GetSceneRoot() => _entities[Constants.MaxPersistentEntities];

    /// <summary>
    /// Get the first persistent entity (index 0).
    /// </summary>
    /// <returns>The persistent root entity.</returns>
    public Entity GetPersistentRoot() => _entities[0];

    /// <summary>
    /// Deactivate an entity by index.
    /// </summary>
    /// <param name="entity">The entity to deactivate.</param>
    public void Deactivate(Entity entity)
    {
        // Check if already deactivated
        if (!_active.HasValue(entity.Index))
        {
            return;
        }
        EventBus<PreEntityDeactivatedEvent>.Push(new(entity));
        _ = _active.Remove(entity.Index);
        _ = _enabled.Remove(entity.Index);
        EventBus<PostEntityDeactivatedEvent>.Push(new(entity));
    }

    /// <summary>
    /// Enable an entity by index.
    /// </summary>
    /// <param name="entityIndex">The entity index.</param>
    public void Enable(Entity entity)
    {
        // Check if already enabled
        if (_enabled.HasValue(entity.Index))
        {
            return;
        }

        if(!_active.HasValue(entity.Index))
        {
            throw new InvalidOperationException(
                $"Tried to enable an entity that doesnt exist, index:{entity.Index}"
            );
        }

        _enabled.Set(entity.Index, true);
        EventBus<EntityEnabledEvent>.Push(
            new(_entities[entity.Index])
        );
    }

    /// <summary>
    /// Disable an entity by index.
    /// </summary>
    /// <param name="entityIndex">The entity index.</param>
    public void Disable(Entity entity)
    {
        // Check if already disabled
        if (!_enabled.Remove(entity.Index))
        {
            return;
        }

        if(!_active.HasValue(entity.Index))
        {
            throw new InvalidOperationException(
                $"Tried to disable an entity that doesnt exist, index:{entity.Index}"
            );
        }

        EventBus<EntityDisabledEvent>.Push(
            new(_entities[entity.Index])
        );
    }

    /// <summary>
    /// Check if an entity is active.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>True if active.</returns>
    public bool IsActive(Entity entity) => _active.HasValue(entity.Index);

    /// <summary>
    /// Check if an entity is active and enabled.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>True if active.</returns>
    public bool IsEnabled(Entity entity) => 
        _active.HasValue(entity.Index) && _enabled.HasValue(entity.Index);
    
    /// <summary>
    /// Get an iterator over active entities.
    /// </summary>
    /// <returns>An enumerable of active entities.</returns>
    public IEnumerable<Entity> GetActiveEntities() => _active
        .Select(a => _entities[a.Index]);

    /// <summary>
    /// Get an iterator over active entities.
    /// </summary>
    /// <returns>An enumerable of active entities.</returns>
    public IEnumerable<Entity> GetEnabledEntities() => _enabled
        .Select(e => _entities[e.Index]);

    /// <summary>
    /// Handle reset event by deactivating only scene entities.
    /// </summary>
    public void OnEvent(ResetEvent _)
    {
        // Deactivate only scene entities (indices >= MaxPersistentEntities)
        var sceneEntities = _active
            .Where(static a => a.Index >= Constants.MaxPersistentEntities)
            .Select(a => _entities[a.Index])
            .ToList();

        foreach (var entity in sceneEntities)
        {
            Deactivate(entity);
        }

        _nextSceneIndex = Constants.MaxPersistentEntities;
    }

    /// <summary>
    /// Handle shutdown event by deactivating ALL entities (both persistent and scene).
    /// Used for complete game shutdown and test cleanup.
    /// </summary>
    public void OnEvent(ShutdownEvent e)
    {
        // Deactivate all entities
        var sceneEntities = _active
            .Select(a => _entities[a.Index])
            .ToList();

        foreach (var entity in sceneEntities)
        {
            Deactivate(entity);
        }

        _nextSceneIndex = Constants.MaxPersistentEntities;
        _nextPersistentIndex = 0;
    }
}
