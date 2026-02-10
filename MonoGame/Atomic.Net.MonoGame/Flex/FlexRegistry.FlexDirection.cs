using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry :
    IEventHandler<BehaviorAddedEvent<FlexDirectionBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexDirectionBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexDirectionBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexDirectionBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexDirectionBehavior>(out var flexDirection))
        {
            node.StyleSetFlexDirection(flexDirection.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexDirectionBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexDirectionBehavior>(out var flexDirection))
        {
            node.StyleSetFlexDirection(flexDirection.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexDirectionBehavior> e)
    {
        // If entity no longer has FlexBehavior, skip - the FlexBehavior removal handler will clean up
        if (!e.Entity.HasBehavior<FlexBehavior>())
        {
            return;
        }

        SetDirtyNode(e.Entity.Index);
    }
}
