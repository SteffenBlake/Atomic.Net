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
            // Check if entity has a flex parent
            var hasFlexParent = HasFlexParent(e.Entity);

            if (val.Value.Percent && hasFlexParent)
            {
                // Has flex parent: use percentage
                node.StyleSetWidthPercent(val.Value.Value);
            }
            else
            {
                // No flex parent OR explicit pixel value: treat as pixels
                node.StyleSetWidth(val.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexWidthBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexWidthBehavior>(out var val))
        {
            // Check if entity has a flex parent
            var hasFlexParent = HasFlexParent(e.Entity);

            if (val.Value.Percent && hasFlexParent)
            {
                // Has flex parent: use percentage
                node.StyleSetWidthPercent(val.Value.Value);
            }
            else
            {
                // No flex parent OR explicit pixel value: treat as pixels
                node.StyleSetWidth(val.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexWidthBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        node.StyleSetWidth(float.NaN);
    }

    // Height
    public void OnEvent(BehaviorAddedEvent<FlexHeightBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexHeightBehavior>(out var val))
        {
            // Check if entity has a flex parent
            var hasFlexParent = HasFlexParent(e.Entity);

            if (val.Value.Percent && hasFlexParent)
            {
                // Has flex parent: use percentage
                node.StyleSetHeightPercent(val.Value.Value);
            }
            else
            {
                // No flex parent OR explicit pixel value: treat as pixels
                node.StyleSetHeight(val.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexHeightBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexHeightBehavior>(out var val))
        {
            // Check if entity has a flex parent
            var hasFlexParent = HasFlexParent(e.Entity);

            if (val.Value.Percent && hasFlexParent)
            {
                // Has flex parent: use percentage
                node.StyleSetHeightPercent(val.Value.Value);
            }
            else
            {
                // No flex parent OR explicit pixel value: treat as pixels
                node.StyleSetHeight(val.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexHeightBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        node.StyleSetHeight(float.NaN);
    }
}
