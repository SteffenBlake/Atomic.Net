using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<FlexDirectionBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexDirectionBehavior>>,
    IEventHandler<BehaviorRemovedEvent<FlexDirectionBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexDirectionBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexDirectionBehavior>(out var flexDirection))
        {
            _nodes[e.Entity.Index]!.StyleSetFlexDirection(flexDirection.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexDirectionBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexDirectionBehavior>(out var flexDirection))
        {
            _nodes[e.Entity.Index]!.StyleSetFlexDirection(flexDirection.Value.Value);
        }
    }

    public void OnEvent(BehaviorRemovedEvent<FlexDirectionBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetFlexDirection(default);
    }
}
