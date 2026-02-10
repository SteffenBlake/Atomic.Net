using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry :
    IEventHandler<BehaviorAddedEvent<FlexJustifyContentBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexJustifyContentBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexJustifyContentBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexJustifyContentBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexJustifyContentBehavior>(out var justify))
        {
            node.StyleSetJustifyContent(justify.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexJustifyContentBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexJustifyContentBehavior>(out var justify))
        {
            node.StyleSetJustifyContent(justify.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexJustifyContentBehavior> e)
    {
        SetDirtyNode(e.Entity.Index);
    }
}
