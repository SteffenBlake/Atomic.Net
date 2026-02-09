using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry :
    IEventHandler<BehaviorAddedEvent<FlexGrowBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexGrowBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexGrowBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexGrowBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexGrowBehavior>(out var grow))
        {
            node.StyleSetFlexGrow(grow.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexGrowBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexGrowBehavior>(out var grow))
        {
            node.StyleSetFlexGrow(grow.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexGrowBehavior> e)
    {
        // Step 1: No guard clause - handler must run
        // Step 6: Pass isRemoval=true to prevent node creation
        EnsureDirtyNode(e.Entity.Index, isRemoval: true);
    }
}
