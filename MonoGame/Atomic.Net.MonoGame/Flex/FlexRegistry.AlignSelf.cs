using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<FlexAlignSelfBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexAlignSelfBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexAlignSelfBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexAlignSelfBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        if (e.Entity.TryGetBehavior<FlexAlignSelfBehavior>(out var alignSelf))
        {
            if (_nodes.TryGetValue(e.Entity.Index, out var node)) { node!.StyleSetAlignSelf(alignSelf.Value.Value); }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexAlignSelfBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        if (e.Entity.TryGetBehavior<FlexAlignSelfBehavior>(out var alignSelf))
        {
            if (_nodes.TryGetValue(e.Entity.Index, out var node)) { node!.StyleSetAlignSelf(alignSelf.Value.Value); }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexAlignSelfBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        if (_nodes.TryGetValue(e.Entity.Index, out var node)) { node!.StyleSetAlignSelf(default); }
    }
}
