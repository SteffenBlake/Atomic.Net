using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<FlexGrowBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexGrowBehavior>>,
    IEventHandler<BehaviorRemovedEvent<FlexGrowBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexGrowBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexGrowBehavior>(out var grow))
        {
            _nodes[e.Entity.Index]!.StyleSetFlexGrow(grow.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexGrowBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexGrowBehavior>(out var grow))
        {
            _nodes[e.Entity.Index]!.StyleSetFlexGrow(grow.Value.Value);
        }
    }

    public void OnEvent(BehaviorRemovedEvent<FlexGrowBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetFlexGrow(float.NaN);
    }
}
