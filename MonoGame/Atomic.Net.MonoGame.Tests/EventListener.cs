using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests;

/// <summary>
/// Generic event listener for testing event firing.
/// </summary>
public sealed class EventListener<TEvent> : IEventHandler<TEvent>
    where TEvent : struct
{
    public List<TEvent> ReceivedEvents { get; } = [];
    
    public void OnEvent(TEvent e)
    {
        ReceivedEvents.Add(e);
    }
    
    public void Clear()
    {
        ReceivedEvents.Clear();
    }
}
