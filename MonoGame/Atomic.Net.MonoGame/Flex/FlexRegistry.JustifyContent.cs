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
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        if (e.Entity.TryGetBehavior<FlexJustifyContentBehavior>(out var justify))
        {
            node.StyleSetJustifyContent(justify.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexJustifyContentBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        if (e.Entity.TryGetBehavior<FlexJustifyContentBehavior>(out var justify))
        {
            node.StyleSetJustifyContent(justify.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexJustifyContentBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        node.StyleSetJustifyContent(default);
    }
}
