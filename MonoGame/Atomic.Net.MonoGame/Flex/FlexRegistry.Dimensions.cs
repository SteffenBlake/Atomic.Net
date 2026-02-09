using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry :
    // Width / Height
    IEventHandler<BehaviorAddedEvent<FlexWidthBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexWidthBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexWidthBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexHeightBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexHeightBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexHeightBehavior>>
{
    // Width
    public void OnEvent(BehaviorAddedEvent<FlexWidthBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexWidthBehavior>(out var val))
        {
            if (val.Value.Percent)
            {
                node.StyleSetWidthPercent(val.Value.Value);
            }
            else
            {
                node.StyleSetWidth(val.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexWidthBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexWidthBehavior>(out var val))
        {
            if (val.Value.Percent)
            {
                node.StyleSetWidthPercent(val.Value.Value);
            }
            else
            {
                node.StyleSetWidth(val.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexWidthBehavior> e)
    {
        // Step 1: No guard clause - handler must run
        // Step 6: Pass isRemoval=true to prevent node creation
        EnsureDirtyNode(e.Entity.Index, isRemoval: true);
    }

    // Height
    public void OnEvent(BehaviorAddedEvent<FlexHeightBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexHeightBehavior>(out var val))
        {
            if (val.Value.Percent)
            {
                node.StyleSetHeightPercent(val.Value.Value);
            }
            else
            {
                node.StyleSetHeight(val.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexHeightBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexHeightBehavior>(out var val))
        {
            if (val.Value.Percent)
            {
                node.StyleSetHeightPercent(val.Value.Value);
            }
            else
            {
                node.StyleSetHeight(val.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexHeightBehavior> e)
    {
        // Step 1: No guard clause - handler must run
        // Step 6: Pass isRemoval=true to prevent node creation
        EnsureDirtyNode(e.Entity.Index, isRemoval: true);
    }
}
