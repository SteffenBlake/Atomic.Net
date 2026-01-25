namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Defines a handler capable of responding to a specific event type.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
public interface IEventHandler<TEvent>
    where TEvent : struct
{
    /// <summary>
    /// Handles an incoming event.
    /// </summary>
    /// <param name="e">The event payload.</param>
    public void OnEvent(TEvent e);
}

