using System.Diagnostics.CodeAnalysis;

namespace Atomic.Net.MonoGame.BED;

/// <summary>
/// Delegate used to initialize or modify a behavior instance.
/// </summary>
/// <typeparam name="TBehavior">The behavior type.</typeparam>
public delegate void BehavorInit<TBehavior>(ref TBehavior behavior)
    where TBehavior : struct;


/// <summary>
/// Central registry for storing and managing behaviors per entity.
/// </summary>
/// <typeparam name="TBehavior">The behavior type.</typeparam>
public class BehaviorRegistry<TBehavior> : 
    ISingleton<BehaviorRegistry<TBehavior>>, 
    IEventHandler<EntityDeactivatedEvent>
    where TBehavior : struct
{
    /// <summary>
    /// Gets the singleton instance of the registry.
    /// </summary>
    public static BehaviorRegistry<TBehavior> Instance { get; } = new();

    /// <summary>
    /// Initializes the registry and registers for entity deactivation events.
    /// </summary>
    public BehaviorRegistry()
    {
        EventBus<EntityDeactivatedEvent>.Register<BehaviorRegistry<TBehavior>>();
    }

    private readonly TBehavior[] _behaviors = new TBehavior[Constants.MaxEntities];
    private readonly bool[] _active = new bool[Constants.MaxEntities];

    /// <summary>
    /// Sets or overwrites a behavior for the given entity.
    /// Only works if the entity itself is active.
    /// </summary>
    /// <param name="entity">The target entity.</param>
    /// <param name="init">Initializer for the behavior.</param>
    public void SetBehavior(Entity entity, BehavorInit<TBehavior> init)
    {
        if (!EntityRegistry.IsActive(entity.Index))
        {
            throw new InvalidOperationException(
                $"Attempted to set behavior '{typeof(TBehavior)}' for inactive entity with id: {entity.Index}."
            );
        }
        var wasActive = _active[entity.Index];
        if (wasActive)
        {
            EventBus<PreBehaviorUpdatedEvent<TBehavior>>.Push(new(entity));
        }

        init(ref _behaviors[entity.Index]);

        _active[entity.Index] = true;

        if (wasActive)
        {
            EventBus<PostBehaviorUpdatedEvent<TBehavior>>.Push(new(entity));
        }
        else
        {
            EventBus<BehaviorAddedEvent<TBehavior>>.Push(new(entity));
        }
    }

    /// <summary>
    /// Removes (deactivates) a behavior for the given entity.
    /// </summary>
    /// <param name="entity">The target entity.</param>
    /// <returns>True if the behavior was removed.</returns>
    public bool Remove(Entity entity)
    {
        if (!_active[entity.Index] || !EntityRegistry.IsActive(entity))
        {
            return false;
        }

        _active[entity.Index] = false;
        EventBus<BehaviorRemovedEvent<TBehavior>>.Push(new(entity));

        return true;
    }

    /// <summary>
    /// Removes (deactivates) a behavior by entity index.
    /// </summary>
    /// <param name="entityIndex">The entity index.</param>
    /// <returns>True if the behavior was removed.</returns>
    public bool Remove(ushort entityIndex) => Remove(new Entity(entityIndex));

    /// <summary>
    /// Attempts to retrieve a behavior for an entity.
    /// </summary>
    /// <param name="entity">The target entity.</param>
    /// <param name="behavior">The behavior instance.</param>
    /// <returns>True if the behavior exists and is active.</returns>
    public bool TryGetBehavior(
        Entity entity, 
        [NotNullWhen(true)]
        out TBehavior? behavior
    )
    {
        return TryGetBehavior(entity.Index, out behavior);
    }

    /// <summary>
    /// Attempts to retrieve a behavior by entity index.
    /// </summary>
    /// <param name="entityIndex">The entity index.</param>
    /// <param name="behavior">The behavior instance.</param>
    /// <returns>True if the behavior exists and is active.</returns>
    public bool TryGetBehavior(
        ushort entityIndex, 
        [NotNullWhen(true)]
        out TBehavior? behavior
    )
    {
        if (!_active[entityIndex])
        {
            behavior = default;
            return false;
        }

        behavior = _behaviors[entityIndex];
        return true;
    }

    /// <summary>
    /// Iterator over active behaviors (entity + behavior).
    /// </summary>
    /// <returns>An enumerable of active entity-behavior pairs.</returns>
    public IEnumerable<(Entity, TBehavior)> GetActiveBehaviors() => _active
        .Index()
        .Where(a => a.Item)
        .Where(a => EntityRegistry.IsActive((ushort)a.Index))
        .Select(a => (
            EntityRegistry.All[a.Index], 
            _behaviors[a.Index]
        ));

    /// <summary>
    /// Handles entity deactivation by removing its behavior.
    /// </summary>
    /// <param name="e">The deactivation event.</param>
    public void OnEvent(EntityDeactivatedEvent e)
    {
        Remove(e.Entity.Index);
    }

    /// <summary>
    /// Exposes behavior active flags as read-only span.
    /// </summary>
    public ReadOnlySpan<bool> Active => _active;
}

