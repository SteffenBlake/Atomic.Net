using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<FlexWrapBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexWrapBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexWrapBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexWrapBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        if (e.Entity.TryGetBehavior<FlexWrapBehavior>(out var wrap))
        {
            if (_nodes.TryGetValue(e.Entity.Index, out var node)) { node!.StyleSetFlexWrap(wrap.Value.Value); }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexWrapBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        if (e.Entity.TryGetBehavior<FlexWrapBehavior>(out var wrap))
        {
            if (_nodes.TryGetValue(e.Entity.Index, out var node)) { node!.StyleSetFlexWrap(wrap.Value.Value); }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexWrapBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        if (_nodes.TryGetValue(e.Entity.Index, out var node)) { node!.StyleSetFlexWrap(default); }
    }
}
