using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED;

public readonly struct PreBehaviorUpdatedEvent<TBehavior>(Entity entity)
    where TBehavior : struct
{
    public readonly Entity Entity = entity;
}

