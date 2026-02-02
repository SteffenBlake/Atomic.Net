using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<FlexAlignItemsBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexAlignItemsBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexAlignItemsBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexAlignItemsBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        if (e.Entity.TryGetBehavior<FlexAlignItemsBehavior>(out var align))
        {
            node.StyleSetAlignItems(align.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexAlignItemsBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        if (e.Entity.TryGetBehavior<FlexAlignItemsBehavior>(out var align))
        {
            node.StyleSetAlignItems(align.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexAlignItemsBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        node.StyleSetAlignItems(default);
    }
}
