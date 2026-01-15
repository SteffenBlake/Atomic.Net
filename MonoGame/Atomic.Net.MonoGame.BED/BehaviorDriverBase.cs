using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED;

/// <summary>
/// Base class for driving behavior execution in response to events.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
/// <typeparam name="TBehavior">The behavior type.</typeparam>
public abstract class BehaviorDriverBase<TEvent, TBehavior>
: IEventHandler<TEvent>
    where TEvent : struct
    where TBehavior : struct
{
    /// <summary>
    /// Handles an event and executes logic for all active behaviors.
    /// </summary>
    /// <param name="e">The event payload.</param>
    public void OnEvent(TEvent e)
    {
        foreach(var (entity, behavior) in BehaviorRegistry<TBehavior>.Instance.GetActiveBehaviors())
        {
            RunInternal(e, entity, behavior);
        }
    }

    /// <summary>
    /// Executes behavior-specific logic for a single entity.
    /// </summary>
    /// <param name="e">The event payload.</param>
    /// <param name="entity">The target entity.</param>
    /// <param name="behavior">The behavior instance.</param>
    protected abstract void RunInternal(
        TEvent e, Entity entity, TBehavior behavior
    );
}

