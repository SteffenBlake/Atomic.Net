using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<FlexGrowBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexGrowBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexGrowBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexGrowBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        if (e.Entity.TryGetBehavior<FlexGrowBehavior>(out var grow))
        {
            if (_nodes.TryGetValue(e.Entity.Index, out var node)) { node!.StyleSetFlexGrow(grow.Value.Value); }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexGrowBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        if (e.Entity.TryGetBehavior<FlexGrowBehavior>(out var grow))
        {
            if (_nodes.TryGetValue(e.Entity.Index, out var node)) { node!.StyleSetFlexGrow(grow.Value.Value); }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexGrowBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        if (_nodes.TryGetValue(e.Entity.Index, out var node)) { node!.StyleSetFlexGrow(float.NaN); }
    }
}
