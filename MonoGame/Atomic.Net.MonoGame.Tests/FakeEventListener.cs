using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests;

/// <summary>
/// Fake event listener for testing event firing.
/// Automatically subscribes on construction and unsubscribes on disposal.
/// </summary>
public sealed class FakeEventListener<TEvent> : IEventHandler<TEvent>, IDisposable
    where TEvent : struct
{
    public List<TEvent> ReceivedEvents { get; } = [];
    
    public FakeEventListener()
    {
        EventBus<TEvent>.Register(this);
    }
    
    public void OnEvent(TEvent e)
    {
        ReceivedEvents.Add(e);
    }
    
    public void Clear()
    {
        ReceivedEvents.Clear();
    }
    
    public void Dispose()
    {
        EventBus<TEvent>.Unregister(this);
    }
}
