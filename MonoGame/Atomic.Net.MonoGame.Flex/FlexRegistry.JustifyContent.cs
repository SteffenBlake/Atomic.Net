using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<JustifyContentBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<JustifyContentBehavior>>,
    IEventHandler<BehaviorRemovedEvent<JustifyContentBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<JustifyContentBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<JustifyContentBehavior>(out var justify))
        {
            _nodes[e.Entity.Index]!.StyleSetJustifyContent(justify.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<JustifyContentBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<JustifyContentBehavior>(out var justify))
        {
            _nodes[e.Entity.Index]!.StyleSetJustifyContent(justify.Value.Value);
        }
    }

    public void OnEvent(BehaviorRemovedEvent<JustifyContentBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetJustifyContent(default);
    }
}
