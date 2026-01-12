namespace Atomic.Net.MonoGame.BED.Hierarchy;

/// <summary>
/// Handles the post-update event of a Parent behavior.
/// Tracks the child under its new parent in the hierarchy registry after the update.
/// </summary>
public sealed class ParentPostUpdatedDriver : IEventHandler<PostBehaviorUpdatedEvent<Parent>>, ISingleton<ParentPostUpdatedDriver>
{
    public static ParentPostUpdatedDriver Instance { get; } = new();

    public void OnEvent(PostBehaviorUpdatedEvent<Parent> e)
    {
        if (BehaviorRegistry<Parent>.Instance.TryGetBehavior(e.Entity, out var newBehavior))
        {
            HierarchyRegistry.TrackChild(new Entity(newBehavior.Value.ParentIndex), e.Entity);
        }
    }
}


