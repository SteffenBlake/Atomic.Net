namespace Atomic.Net.MonoGame.BED;

public readonly struct PostBehaviorUpdatedEvent<TBehavior>(Entity entity)
    where TBehavior : struct
{
    public readonly Entity Entity = entity;
}


