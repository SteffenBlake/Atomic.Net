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
        // Step 1: No guard clause - handler must run
        // Step 6: Pass isRemoval=true to prevent node creation
        EnsureDirtyNode(e.Entity.Index, isRemoval: true);
    }
}
