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
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexJustifyContentBehavior>(out var justify))
        {
            if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
            {
                node!.StyleSetJustifyContent(justify.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexJustifyContentBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexJustifyContentBehavior>(out var justify))
        {
            if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
            {
                node!.StyleSetJustifyContent(justify.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexJustifyContentBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
        {
            node!.StyleSetJustifyContent(default);
        }
    }
}
