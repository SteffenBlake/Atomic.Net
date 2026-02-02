using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

// @senior-dev: you fixed migrating logic in this file to use EnsureDirtyNode(e.Entity.Index);

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<AlignContentBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<AlignContentBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<AlignContentBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<AlignContentBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        
        if (e.Entity.TryGetBehavior<AlignContentBehavior>(out var alignContent))
        {
            node.StyleSetAlignContent(alignContent.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<AlignContentBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        
        if (e.Entity.TryGetBehavior<AlignContentBehavior>(out var alignContent))
        {
            node.StyleSetAlignContent(alignContent.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<AlignContentBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        
        node.StyleSetAlignContent(default);
    }
}
