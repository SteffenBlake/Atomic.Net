using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<FlexZOverride>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexZOverride>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexZOverride>>
{
    public void OnEvent(BehaviorAddedEvent<FlexZOverride> e)
    {
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        _dirty.Set(e.Entity.Index, true);
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexZOverride> e)
    {
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        _dirty.Set(e.Entity.Index, true);
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexZOverride> e)
    {
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        _dirty.Set(e.Entity.Index, true);
    }
}
