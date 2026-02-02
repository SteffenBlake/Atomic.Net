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
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        if (e.Entity.TryGetBehavior<FlexDirectionBehavior>(out var flexDirection))
        {
            node.StyleSetFlexDirection(flexDirection.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexDirectionBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        if (e.Entity.TryGetBehavior<FlexDirectionBehavior>(out var flexDirection))
        {
            node.StyleSetFlexDirection(flexDirection.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexDirectionBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        node.StyleSetFlexDirection(default);
    }
}
