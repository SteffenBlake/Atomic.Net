
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
        .Select(index => new Entity((ushort)index))
    ];

    private readonly Entity[] _sceneEntities = [.. 
        Enumerable.Range(0, (int)Constants.MaxSceneEntities)
        .Select(index => new Entity((uint)index))
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

    public Entity this[PartitionIndex index]
    {
        get
        {
            if (index.TryMatch(out ushort? globalNullable) && globalNullable.HasValue)
            {
                return _globalEntities[globalNullable.Value];
            }
            if (index.TryMatch(out uint? sceneNullable) && sceneNullable.HasValue)
            {
                return _sceneEntities[sceneNullable.Value];
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
            PartitionIndex sceneIndex = i;
            
            if (_active.HasValue(sceneIndex))
            {
                continue;
            }

            _active.Set(sceneIndex, true);
            _enabled.Set(sceneIndex, true);
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
            PartitionIndex globalIndex = i;
            
            if (_active.HasValue(globalIndex))
            {
                continue;
            }

            _active.Set(globalIndex, true);
            _enabled.Set(globalIndex, true);
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
    /// Get an iterator over active entities.
    /// </summary>
    /// <returns>An enumerable of active entities.</returns>
    public IEnumerable<Entity> GetActiveEntities()
    {
        foreach (var (index, _) in _active.Global)
        {
            yield return _globalEntities[index];
        }
        foreach (var (index, _) in _active.Scene)
        {
            yield return _sceneEntities[index];
        }
    }

    /// <summary>
    /// Get an iterator over enabled entities.
    /// </summary>
    /// <returns>An enumerable of enabled entities.</returns>
    public IEnumerable<Entity> GetEnabledEntities()
    {
        foreach (var (index, _) in _enabled.Global)
        {
            yield return _globalEntities[index];
        }
        foreach (var (index, _) in _enabled.Scene)
        {
            yield return _sceneEntities[index];
        }
    }

    /// <summary>
    /// Handle reset event by deactivating only scene entities.
    /// </summary>
    public void OnEvent(ResetEvent _)
    {
        // Deactivate only scene entities - iterate over scene partition
        var sceneEntities = _active.Scene
            .Select(a => _sceneEntities[a.Index])
            .ToList();

        foreach (var entity in sceneEntities)
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
        var allEntities = new List<Entity>();
        
        foreach (var (index, _) in _active.Global)
        {
            allEntities.Add(_globalEntities[index]);
        }
        foreach (var (index, _) in _active.Scene)
        {
            allEntities.Add(_sceneEntities[index]);
        }

        foreach (var entity in allEntities)
        {
            Deactivate(entity);
        }

        _nextSceneIndex = 0;
        _nextGlobalIndex = 0;
    }
}
