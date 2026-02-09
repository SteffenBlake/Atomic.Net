using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry :
    IEventHandler<BehaviorAddedEvent<FlexShrinkBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexShrinkBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexShrinkBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexShrinkBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexShrinkBehavior>(out var shrink))
        {
            node.StyleSetFlexShrink(shrink.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexShrinkBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexShrinkBehavior>(out var shrink))
        {
            node.StyleSetFlexShrink(shrink.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexShrinkBehavior> e)
    {
        // Step 1: No guard clause - handler must run
        // Step 6: Pass isRemoval=true to prevent node creation
        EnsureDirtyNode(e.Entity.Index, isRemoval: true);
    }
}
