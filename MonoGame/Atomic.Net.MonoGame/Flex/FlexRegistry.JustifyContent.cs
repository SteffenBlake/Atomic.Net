using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<FlexJustifyContentBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexJustifyContentBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexJustifyContentBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexJustifyContentBehavior> e)
    {
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexJustifyContentBehavior>(out var justify))
        {
            _nodes[e.Entity.Index.ToInt()]!.StyleSetJustifyContent(justify.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexJustifyContentBehavior> e)
    {
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexJustifyContentBehavior>(out var justify))
        {
            _nodes[e.Entity.Index.ToInt()]!.StyleSetJustifyContent(justify.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexJustifyContentBehavior> e)
    {
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index.ToInt()]!.StyleSetJustifyContent(default);
    }
}
