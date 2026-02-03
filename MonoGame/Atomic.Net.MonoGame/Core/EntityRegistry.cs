
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

    private readonly Entity[] _globalEntities = [.. 
        Enumerable.Range(0, Constants.MaxGlobalEntities)
        .Select(static index => new Entity((ushort)index))
    ];

    private readonly Entity[] _sceneEntities = [.. 
        Enumerable.Range(0, (int)Constants.MaxSceneEntities)
        .Select(static index => new Entity((uint)index))
    ];

    private readonly PartitionedSparseArray<bool> _active = new(
        Constants.MaxGlobalEntities,
        Constants.MaxSceneEntities
    );
    private readonly PartitionedSparseArray<bool> _enabled = new(
        Constants.MaxGlobalEntities,
        Constants.MaxSceneEntities
    );
    private uint _nextSceneIndex = 0;
    private ushort _nextGlobalIndex = 0;
    
    // Pre-allocated lists for zero-allocation event handlers
    private readonly List<Entity> _tempSceneEntityList = new((int)Constants.MaxSceneEntities);
    private readonly List<Entity> _tempAllEntityList = new((int)Constants.MaxSceneEntities + Constants.MaxGlobalEntities);

    public Entity this[PartitionIndex index]
    {
        get
        {
            if (index.TryMatch(out ushort globalIdx))
            {
                return _globalEntities[globalIdx];
            }
            if (index.TryMatch(out uint sceneIdx))
            {
                return _sceneEntities[sceneIdx];
            }
            throw new InvalidOperationException("Invalid PartitionIndex state");
        }
    }

    /// <summary>
    /// Activate the next available scene entity.
    /// </summary>
    /// <returns>The activated entity.</returns>
    public Entity Activate()
    {
        for (uint offset = 0; offset < Constants.MaxSceneEntities; offset++)
        {
            uint i = (_nextSceneIndex + offset) % Constants.MaxSceneEntities;
            
            // Direct access to Scene array for scene-specific logic (performance)
            if (_active.Scene.HasValue(i))
            {
                continue;
            }

            _active.Scene.Set(i, true);
            _enabled.Scene.Set(i, true);
            _nextSceneIndex = (i + 1) % Constants.MaxSceneEntities;

            return _sceneEntities[i];
        }

        throw new InvalidOperationException("MaxSceneEntities reached");
    }

    /// <summary>
    /// Activate the next available global entity.
    /// </summary>
    /// <returns>The activated entity.</returns>
    public Entity ActivateGlobal()
    {
        for (ushort offset = 0; offset < Constants.MaxGlobalEntities; offset++)
        {
            ushort i = (ushort)((_nextGlobalIndex + offset) % Constants.MaxGlobalEntities);
            
            // Direct access to Global array for global-specific logic (performance)
            if (_active.Global.HasValue(i))
            {
                continue;
            }

            _active.Global.Set(i, true);
            _enabled.Global.Set(i, true);
            _nextGlobalIndex = (ushort)((i + 1) % Constants.MaxGlobalEntities);

            return _globalEntities[i];
        }

        throw new InvalidOperationException("MaxGlobalEntities reached");
    }

    /// <summary>
    /// Get the first scene entity (index 0 in scene partition).
    /// </summary>
    /// <returns>The scene root entity.</returns>
    public Entity GetSceneRoot() => _sceneEntities[0];

    /// <summary>
    /// Get the first global entity (index 0 in global partition).
    /// </summary>
    /// <returns>The global root entity.</returns>
    public Entity GetGlobalRoot() => _globalEntities[0];

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
    /// <param name="entity">The entity.</param>
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
                $"Tried to enable an entity that doesnt exist"
            );
        }

        _enabled.Set(entity.Index, true);
        EventBus<EntityEnabledEvent>.Push(new(entity));
    }

    /// <summary>
    /// Disable an entity by index.
    /// </summary>
    /// <param name="entity">The entity.</param>
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
                $"Tried to disable an entity that doesnt exist"
            );
        }

        EventBus<EntityDisabledEvent>.Push(new(entity));
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
    /// Get an iterator over active global entities.
    /// </summary>
    /// <returns>An enumerable of active global entities.</returns>
    public IEnumerable<Entity> GetActiveGlobalEntities()
    {
        foreach (var (index, _) in _active.Global)
        {
            yield return _globalEntities[index];
        }
    }
    
    /// <summary>
    /// Get an iterator over active scene entities.
    /// </summary>
    /// <returns>An enumerable of active scene entities.</returns>
    public IEnumerable<Entity> GetActiveSceneEntities()
    {
        foreach (var (index, _) in _active.Scene)
        {
            yield return _sceneEntities[index];
        }
    }
    
    /// <summary>
    /// Get an iterator over enabled global entities.
    /// </summary>
    /// <returns>An enumerable of enabled global entities.</returns>
    public IEnumerable<Entity> GetEnabledGlobalEntities()
    {
        foreach (var (index, _) in _enabled.Global)
        {
            yield return _globalEntities[index];
        }
    }
    
    /// <summary>
    /// Get an iterator over enabled scene entities.
    /// </summary>
    /// <returns>An enumerable of enabled scene entities.</returns>
    public IEnumerable<Entity> GetEnabledSceneEntities()
    {
        foreach (var (index, _) in _enabled.Scene)
        {
            yield return _sceneEntities[index];
        }
    }
    
    /// <summary>
    /// Get an iterator over all enabled entities (both global and scene).
    /// </summary>
    /// <returns>An enumerable of enabled entities.</returns>
    public IEnumerable<Entity> GetEnabledEntities()
    {
        foreach (var entity in GetEnabledGlobalEntities())
        {
            yield return entity;
        }
        foreach (var entity in GetEnabledSceneEntities())
        {
            yield return entity;
        }
    }

    /// <summary>
    /// Handle reset event by deactivating only scene entities.
    /// </summary>
    public void OnEvent(ResetEvent _)
    {
        // Deactivate only scene entities - iterate over scene partition
        // Use pre-allocated list to avoid allocations during reset
        _tempSceneEntityList.Clear();
        
        foreach (var (index, _) in _active.Scene)
        {
            _tempSceneEntityList.Add(_sceneEntities[index]);
        }

        foreach (var entity in _tempSceneEntityList)
        {
            Deactivate(entity);
        }

        _nextSceneIndex = 0;
    }

    /// <summary>
    /// Handle shutdown event by deactivating ALL entities (both global and scene).
    /// Used for complete game shutdown and test cleanup.
    /// </summary>
    public void OnEvent(ShutdownEvent e)
    {
        // Deactivate all entities (both global and scene)
        // Use pre-allocated list to avoid allocations during shutdown
        _tempAllEntityList.Clear();
        
        foreach (var (index, _) in _active.Global)
        {
            _tempAllEntityList.Add(_globalEntities[index]);
        }
        foreach (var (index, _) in _active.Scene)
        {
            _tempAllEntityList.Add(_sceneEntities[index]);
        }

        foreach (var entity in _tempAllEntityList)
        {
            Deactivate(entity);
        }

        _nextSceneIndex = 0;
        _nextGlobalIndex = 0;
    }
}
