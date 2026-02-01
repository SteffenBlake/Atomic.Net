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
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        if (e.Entity.TryGetBehavior<FlexDirectionBehavior>(out var flexDirection))
        {
            if (_nodes.TryGetValue(e.Entity.Index, out var node)) { node!.StyleSetFlexDirection(flexDirection.Value.Value); }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexDirectionBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        if (e.Entity.TryGetBehavior<FlexDirectionBehavior>(out var flexDirection))
        {
            if (_nodes.TryGetValue(e.Entity.Index, out var node)) { node!.StyleSetFlexDirection(flexDirection.Value.Value); }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexDirectionBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index)) { _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode(); }
        if (_nodes.TryGetValue(e.Entity.Index, out var node)) { node!.StyleSetFlexDirection(default); }
    }
}
