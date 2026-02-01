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
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexAlignItemsBehavior>(out var align))
        {
            if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
            {
                node!.StyleSetAlignItems(align.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexAlignItemsBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexAlignItemsBehavior>(out var align))
        {
            if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
            {
                node!.StyleSetAlignItems(align.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexAlignItemsBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
        {
            node!.StyleSetAlignItems(default);
        }
    }
}
