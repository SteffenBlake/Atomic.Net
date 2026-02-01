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
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexDirectionBehavior>(out var flexDirection))
        {
            _nodes[e.Entity.Index.ToInt()]!.StyleSetFlexDirection(flexDirection.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexDirectionBehavior> e)
    {
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexDirectionBehavior>(out var flexDirection))
        {
            _nodes[e.Entity.Index.ToInt()]!.StyleSetFlexDirection(flexDirection.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexDirectionBehavior> e)
    {
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index.ToInt()]!.StyleSetFlexDirection(default);
    }
}
