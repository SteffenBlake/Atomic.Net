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
        // Step 1: No guard clause - handler must run
        // Step 6: Pass isRemoval=true to prevent node creation
        EnsureDirtyNode(e.Entity.Index, isRemoval: true);
    }
}
