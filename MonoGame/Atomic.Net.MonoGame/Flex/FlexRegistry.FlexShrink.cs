using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<FlexShrinkBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexShrinkBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexShrinkBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexShrinkBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        if (e.Entity.TryGetBehavior<FlexShrinkBehavior>(out var shrink))
        {
            if (_nodes.TryGetValue(e.Entity.Index, out var node)) { node!.StyleSetFlexShrink(shrink.Value.Value); }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexShrinkBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        if (e.Entity.TryGetBehavior<FlexShrinkBehavior>(out var shrink))
        {
            if (_nodes.TryGetValue(e.Entity.Index, out var node)) { node!.StyleSetFlexShrink(shrink.Value.Value); }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexShrinkBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        if (_nodes.TryGetValue(e.Entity.Index, out var node)) { node!.StyleSetFlexShrink(float.NaN); }
    }
}
