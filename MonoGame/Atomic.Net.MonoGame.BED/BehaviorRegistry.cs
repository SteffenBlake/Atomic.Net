using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED;

/// <summary>
/// Central registry for storing and managing behaviors per entity.
/// </summary>
/// <typeparam name="TBehavior">The behavior type.</typeparam>
public class BehaviorRegistry<TBehavior> : 
    ISingleton<BehaviorRegistry<TBehavior>>, 
    IEventHandler<InitializeEvent>,
    IEventHandler<PreEntityDeactivatedEvent>
    where TBehavior : struct
{
    public static void Initialize()
    {
        EventBus<InitializeEvent>.Register<BehaviorRegistry<TBehavior>>();
    }

    public static BehaviorRegistry<TBehavior> Instance { get; } = new();


    /// <summary>
    /// Handles initialization by registering for entity deactivation events.
    /// </summary>
    public void OnEvent(InitializeEvent _)
    {
        EventBus<PreEntityDeactivatedEvent>.Register(this);
    }

    private readonly SparseArray<TBehavior> _behaviors = new(Constants.MaxEntities);

    /// <summary>
    /// Sets or replaces a regular (non-backed) behavior.
    /// </summary>
    /// <param name="entity">The target entity.</param>
    /// <param name="mutate">Action to mutate the behavior by reference.</param>
    public void SetBehavior(Entity entity, RefAction<TBehavior> mutate)
    {
        if (!entity.Active)
        {
            throw new InvalidOperationException(
                $"Attempted to set behavior '{typeof(TBehavior)}' for inactive entity with id: {entity.Index}."
            );
        }
        var wasBehaviorActive = _behaviors.HasValue(entity.Index);
        if (wasBehaviorActive)
        {
            EventBus<PreBehaviorUpdatedEvent<TBehavior>>.Push(new(entity));
        }

        using var behaviorRef = _behaviors.GetMut(entity.Index);
        mutate(ref behaviorRef.Value);

        if (wasBehaviorActive)
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
        if (!_behaviors.HasValue(entity.Index))
        {
            return false;
        }

        EventBus<PreBehaviorRemovedEvent<TBehavior>>.Push(new(entity));

        _ = _behaviors.Remove(entity.Index);
        
        EventBus<PostBehaviorRemovedEvent<TBehavior>>.Push(new(entity));

        return true;
    }

    /// <summary>
    /// Attempts to retrieve a behavior for an entity.
    /// </summary>
    /// <param name="entity">The target entity.</param>
    /// <param name="behavior">The behavior instance.</param>
    /// <returns>True if the behavior exists and is active.</returns>
    /// <exception cref="InvalidOperationException">Thrown when entity is not active.</exception>
    public bool TryGetBehavior(
        Entity entity, 
        [NotNullWhen(true)]
        out TBehavior? behavior
    )
    {
        if (!entity.Active)
        {
            throw new InvalidOperationException(
                $"Cannot get behavior '{typeof(TBehavior)}' for inactive entity with id: {entity.Index}."
            );
        }
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
        return _behaviors.TryGetValue(entityIndex, out behavior);
    }

    /// <summary>
    /// Check if an entity has this behavior 
    /// </summary>
    /// <returns>Whether the entity has the behavior or not</returns>
    /// <exception cref="InvalidOperationException">Thrown when entity is not active.</exception>
    public bool HasBehavior(Entity entity)
    {
        if (!entity.Active)
        {
            throw new InvalidOperationException(
                $"Cannot check behavior '{typeof(TBehavior)}' for inactive entity with id: {entity.Index}."
            );
        }
        return _behaviors.HasValue(entity.Index);
    }

    /// <summary>
    /// Iterator over active behaviors (entity + behavior).
    /// </summary>
    /// <returns>An enumerable of active entity-behavior pairs.</returns>
    public IEnumerable<(Entity Entity, TBehavior Behavior)> GetActiveBehaviors() => 
        _behaviors
        .Select(a => (
            EntityRegistry.Instance[a.Index],
            a.Value
        ));

    /// <summary>
    /// Handles entity deactivation by removing its behavior.
    /// </summary>
    /// <param name="e">The deactivation event.</param>
    public void OnEvent(PreEntityDeactivatedEvent e)
    {
        Remove(e.Entity);
    }
}
