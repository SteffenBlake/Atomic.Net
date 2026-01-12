using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<FlexWrapBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexWrapBehavior>>,
    IEventHandler<BehaviorRemovedEvent<FlexWrapBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexWrapBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexWrapBehavior>(out var wrap))
        {
            _nodes[e.Entity.Index]!.StyleSetFlexWrap(wrap.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexWrapBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexWrapBehavior>(out var wrap))
        {
            _nodes[e.Entity.Index]!.StyleSetFlexWrap(wrap.Value.Value);
        }
    }

    public void OnEvent(BehaviorRemovedEvent<FlexWrapBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetFlexWrap(default);
    }
}
