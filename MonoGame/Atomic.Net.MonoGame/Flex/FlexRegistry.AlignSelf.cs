using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry :
    IEventHandler<BehaviorAddedEvent<FlexAlignSelfBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexAlignSelfBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexAlignSelfBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexAlignSelfBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexAlignSelfBehavior>(out var alignSelf))
        {
            node.StyleSetAlignSelf(alignSelf.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexAlignSelfBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexAlignSelfBehavior>(out var alignSelf))
        {
            node.StyleSetAlignSelf(alignSelf.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexAlignSelfBehavior> e)
    {
        // If entity no longer has FlexBehavior, skip - the FlexBehavior removal handler will clean up
        if (!e.Entity.HasBehavior<FlexBehavior>())
        {
            return;
        }

        SetDirtyNode(e.Entity.Index);
    }
}
