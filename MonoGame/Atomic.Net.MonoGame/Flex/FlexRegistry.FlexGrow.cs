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
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        if (e.Entity.TryGetBehavior<FlexGrowBehavior>(out var grow))
        {
            node.StyleSetFlexGrow(grow.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexGrowBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        if (e.Entity.TryGetBehavior<FlexGrowBehavior>(out var grow))
        {
            node.StyleSetFlexGrow(grow.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexGrowBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        node.StyleSetFlexGrow(float.NaN);
    }
}
