namespace Atomic.Net.MonoGame.BED;

/// <summary>
/// Central registry for entity lifecycle management.
/// </summary>
public static class EntityRegistry
{
    private static readonly Entity[] _entities = [.. 
        Enumerable.Range(0, Constants.MaxEntities - 1)
        .Select(index => new Entity((ushort)index))
    ];

    private static readonly bool[] _active = new bool[Constants.MaxEntities];
    private static readonly bool[] _enabled = new bool[Constants.MaxEntities];
    private static ushort _nextIndex = 0;

    /// <summary>
    /// Activate the next available entity.
    /// </summary>
    /// <returns>The activated entity.</returns>
    public static Entity Activate()
    {
        for (ushort offset = 0; offset < Constants.MaxEntities; offset++)
        {
            ushort i = (ushort)((_nextIndex + offset) % Constants.MaxEntities);
            if (_active[i])
            {
                continue;
            }

            _active[i] = true;
            _enabled[i] = true;
            _nextIndex = (ushort)((i+1) % Constants.MaxEntities);

            return new(i);
        }

        throw new InvalidOperationException("MaxEntities reached");
    }

    /// <summary>
    /// Deactivate an entity by index.
    /// </summary>
    /// <param name="entityIndex">The entity index.</param>
    public static void Deactivate(ushort entityIndex)
    {
        // Check if already deactivated
        if (!_active[entityIndex])
        {
            return;
        }

        _active[entityIndex] = false;
        _enabled[entityIndex] = false;
        EventBus<EntityDeactivatedEvent>.Push(
            new(_entities[entityIndex])
        );
    }

    /// <summary>
    /// Enable an entity by index.
    /// </summary>
    /// <param name="entityIndex">The entity index.</param>
    public static void Enable(ushort entityIndex)
    {
        // Check if already enabled
        if (_enabled[entityIndex])
        {
            return;
        }

        if(!_active[entityIndex])
        {
            throw new InvalidOperationException(
                $"Tried to enable an entity that doesnt exist, index:{entityIndex}"
            );
        }

        _enabled[entityIndex] = true;
        EventBus<EntityEnabledEvent>.Push(
            new(_entities[entityIndex])
        );
    }

    /// <summary>
    /// Disable an entity by index.
    /// </summary>
    /// <param name="entityIndex">The entity index.</param>
    public static void Disable(ushort entityIndex)
    {
        // Check if already disabled
        if (!_enabled[entityIndex])
        {
            return;
        }

        if(!_active[entityIndex])
        {
            throw new InvalidOperationException(
                $"Tried to disable an entity that doesnt exist, index:{entityIndex}"
            );
        }

        _enabled[entityIndex] = false;
        EventBus<EntityDisabledEvent>.Push(
            new(_entities[entityIndex])
        );
    }

    /// <summary>
    /// Check if an entity is active.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>True if active.</returns>
    public static bool IsActive(Entity entity) => _active[entity.Index];
    
    /// <summary>
    /// Check if an entity is active.
    /// </summary>
    /// <param name="entityIndex">The entity index.</param>
    /// <returns>True if active.</returns>
    public static bool IsActive(ushort entityIndex) => _active[entityIndex];

    /// <summary>
    /// Check if an entity is active and enabled.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>True if active.</returns>
    public static bool IsEnabled(Entity entity) => 
        _active[entity.Index] && _enabled[entity.Index];
    
    /// <summary>
    /// Check if an entity is active and enabled.
    /// </summary>
    /// <param name="entityIndex">The entity index.</param>
    /// <returns>True if active.</returns>
    public static bool IsEnabled(ushort entityIndex) => 
        _active[entityIndex] && _enabled[entityIndex];

    /// <summary>
    /// Get an iterator over active entities.
    /// </summary>
    /// <returns>An enumerable of active entities.</returns>
    public static IEnumerable<Entity> GetActiveEntities() => _active
        .Index()
        .Where(a => a.Item)
        .Select(a => _entities[a.Index]);

    /// <summary>
    /// Get an iterator over active entities.
    /// </summary>
    /// <returns>An enumerable of active entities.</returns>
    public static IEnumerable<Entity> GetEnabledEntities() => _active
        .Index()
        .Where(a => a.Item && _enabled[a.Index])
        .Select(a => _entities[a.Index]);

    /// <summary>
    /// Expose active flags as read-only span.
    /// </summary>
    public static ReadOnlySpan<bool> Active => _active;

    /// <summary>
    /// Expose active flags as read-only span.
    /// </summary>
    public static ReadOnlySpan<bool> Enabled => _enabled;

    /// <summary>
    /// Expose all entities as read-only span.
    /// </summary>
    public static ReadOnlySpan<Entity> All => _entities;
}

