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
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexShrinkBehavior>(out var shrink))
        {
            _nodes[e.Entity.Index.ToInt()]!.StyleSetFlexShrink(shrink.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexShrinkBehavior> e)
    {
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexShrinkBehavior>(out var shrink))
        {
            _nodes[e.Entity.Index.ToInt()]!.StyleSetFlexShrink(shrink.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexShrinkBehavior> e)
    {
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index.ToInt()]!.StyleSetFlexShrink(float.NaN);
    }
}
