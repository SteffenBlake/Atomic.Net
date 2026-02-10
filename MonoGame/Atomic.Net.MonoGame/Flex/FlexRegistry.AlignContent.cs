using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry :
    IEventHandler<BehaviorAddedEvent<AlignContentBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<AlignContentBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<AlignContentBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<AlignContentBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);

        if (e.Entity.TryGetBehavior<AlignContentBehavior>(out var alignContent))
        {
            node.StyleSetAlignContent(alignContent.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<AlignContentBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);

        if (e.Entity.TryGetBehavior<AlignContentBehavior>(out var alignContent))
        {
            node.StyleSetAlignContent(alignContent.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<AlignContentBehavior> e)
    {
        // If entity no longer has FlexBehavior, skip - the FlexBehavior removal handler will clean up
        if (!e.Entity.HasBehavior<FlexBehavior>())
        {
            return;
        }

        SetDirtyNode(e.Entity.Index);
    }
}
