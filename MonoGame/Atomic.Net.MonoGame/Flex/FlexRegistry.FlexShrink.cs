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
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        if (e.Entity.TryGetBehavior<FlexShrinkBehavior>(out var shrink))
        {
            node.StyleSetFlexShrink(shrink.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexShrinkBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        if (e.Entity.TryGetBehavior<FlexShrinkBehavior>(out var shrink))
        {
            node.StyleSetFlexShrink(shrink.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexShrinkBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        node.StyleSetFlexShrink(float.NaN);
    }
}
