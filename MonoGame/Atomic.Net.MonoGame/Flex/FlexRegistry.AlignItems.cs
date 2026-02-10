using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry :
    IEventHandler<BehaviorAddedEvent<FlexAlignItemsBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexAlignItemsBehavior>>,
    IEventHandler<PostBehaviorRemovedEvent<FlexAlignItemsBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexAlignItemsBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexAlignItemsBehavior>(out var align))
        {
            node.StyleSetAlignItems(align.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexAlignItemsBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexAlignItemsBehavior>(out var align))
        {
            node.StyleSetAlignItems(align.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorRemovedEvent<FlexAlignItemsBehavior> e)
    {
        SetDirtyNode(e.Entity.Index);
    }
}
