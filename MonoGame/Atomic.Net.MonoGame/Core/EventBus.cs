namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Static event bus for dispatching value-type events to registered handlers.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
public static class EventBus<TEvent>
    where TEvent : struct
{
    /// <summary>
    /// Minimal delegate used for event dispatch.
    /// </summary>
    private delegate void MinimalEvent(TEvent e);
    private static MinimalEvent? _onPush;

    /// <summary>
    /// Registers a singleton handler for this event type.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    public static void Register<THandler>(THandler handler)
        where THandler : IEventHandler<TEvent>
    {
        _onPush += handler.OnEvent;
    }

    /// <summary>
    /// Unregisters a singleton handler for this event type.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    public static void Unregister<THandler>()
        where THandler : ISingleton<THandler>, IEventHandler<TEvent>
    {
        Unregister(THandler.Instance);
    }

    /// <summary>
    /// Unregisters a handler for this event type.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    public static void Unregister<THandler>(THandler handler)
        where THandler : IEventHandler<TEvent>
    {
        _onPush -= handler.OnEvent;
    }

    /// <summary>
    /// Pushes an event to all registered handlers.
    /// </summary>
    /// <param name="e">The event payload.</param>
    public static void Push(TEvent e)
    {
        _onPush?.Invoke(e);
    }
}

