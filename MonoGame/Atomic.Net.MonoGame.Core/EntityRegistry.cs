
namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Central registry for entity lifecycle management.
/// </summary>
public class EntityRegistry : IEventHandler<ResetEvent>
{
    internal static void Initialize()
    {
        Instance = new();
        EventBus<ResetEvent>.Register(Instance);
    }

    public static EntityRegistry Instance { get; private set; } = null!;

    private readonly Entity[] _entities = [.. 
        Enumerable.Range(0, Constants.MaxEntities - 1)
        .Select(index => new Entity((ushort)index))
    ];

    private readonly SparseArray<bool> _active = new(Constants.MaxEntities);
    private readonly SparseArray<bool> _enabled = new(Constants.MaxEntities);
    private ushort _nextSceneIndex = Constants.MaxLoadingEntities;
    private ushort _nextLoadingIndex = 0;

    public Entity this[ushort index]
    {
        get
        {
            return _entities[index];
        }
    }

    /// <summary>
    /// Activate the next available scene entity (index &gt;= MaxLoadingEntities).
    /// </summary>
    /// <returns>The activated entity.</returns>
    public Entity Activate()
    {
        for (ushort offset = 0; offset < Constants.MaxEntities - Constants.MaxLoadingEntities; offset++)
        {
            ushort i = (ushort)(Constants.MaxLoadingEntities + 
                ((_nextSceneIndex - Constants.MaxLoadingEntities + offset) % 
                (Constants.MaxEntities - Constants.MaxLoadingEntities)));
            
            if (_active[i])
            {
                continue;
            }

            _active.Set(i, true);
            _enabled.Set(i, true);
            _nextSceneIndex = (ushort)(i + 1);
            if (_nextSceneIndex >= Constants.MaxEntities)
            {
                _nextSceneIndex = Constants.MaxLoadingEntities;
            }

            return new(i);
        }

        throw new InvalidOperationException("MaxEntities (scene partition) reached");
    }

    /// <summary>
    /// Activate the next available loading entity (index &lt; MaxLoadingEntities).
    /// </summary>
    /// <returns>The activated entity.</returns>
    public Entity ActivateLoading()
    {
        for (ushort offset = 0; offset < Constants.MaxLoadingEntities; offset++)
        {
            ushort i = (ushort)((_nextLoadingIndex + offset) % Constants.MaxLoadingEntities);
            
            if (_active[i])
            {
                continue;
            }

            _active.Set(i, true);
            _enabled.Set(i, true);
            _nextLoadingIndex = (ushort)((i + 1) % Constants.MaxLoadingEntities);

            return new(i);
        }

        throw new InvalidOperationException("MaxLoadingEntities reached");
    }

    /// <summary>
    /// Get the first scene entity (index MaxLoadingEntities).
    /// </summary>
    /// <returns>The scene root entity.</returns>
    public Entity GetSceneRoot() => new(Constants.MaxLoadingEntities);

    /// <summary>
    /// Get the first loading entity (index 0).
    /// </summary>
    /// <returns>The loading root entity.</returns>
    public Entity GetLoadingRoot() => new(0);

    /// <summary>
    /// Deactivate an entity by index.
    /// </summary>
    /// <param name="entity">The entity to deactivate.</param>
    public void Deactivate(Entity entity)
    {
        // Check if already deactivated
        if (!_active.Remove(entity.Index))
        {
            return;
        }

        _enabled.Remove(entity.Index);
        EventBus<EntityDeactivatedEvent>.Push(new(entity));
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
        // Deactivate only scene entities (indices >= MaxLoadingEntities)
        var sceneEntities = _active
            .Where(a => a.Index >= Constants.MaxLoadingEntities)
            .Select(a => _entities[a.Index])
            .ToList();

        foreach (var entity in sceneEntities)
        {
            Deactivate(entity);
        }

        _nextSceneIndex = Constants.MaxLoadingEntities;
    }
}
