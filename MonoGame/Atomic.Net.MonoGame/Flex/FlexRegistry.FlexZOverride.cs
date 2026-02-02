using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

// @senior-dev: You still need to migrate this file to use 
// var node = EnsureDirtyNode(e.Entity.Index);
// Syntax. Fix it.

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<FlexZOverride>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexZOverride>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexZOverride>>
{
    public void OnEvent(BehaviorAddedEvent<FlexZOverride> e)
    {
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        _dirty.Set(e.Entity.Index, true);
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexZOverride> e)
    {
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        _dirty.Set(e.Entity.Index, true);
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexZOverride> e)
    {
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = node;
        }
        _dirty.Set(e.Entity.Index, true);
    }
}
