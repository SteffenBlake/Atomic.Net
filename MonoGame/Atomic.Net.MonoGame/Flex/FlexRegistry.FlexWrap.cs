using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry :
    IEventHandler<BehaviorAddedEvent<FlexWrapBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexWrapBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexWrapBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexWrapBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexWrapBehavior>(out var wrap))
        {
            node.StyleSetFlexWrap(wrap.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexWrapBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexWrapBehavior>(out var wrap))
        {
            node.StyleSetFlexWrap(wrap.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexWrapBehavior> e)
    {
        // If entity no longer has FlexBehavior, skip - the FlexBehavior removal handler will clean up
        if (!e.Entity.HasBehavior<FlexBehavior>())
        {
            return;
        }

        SetDirtyNode(e.Entity.Index);
    }
}
