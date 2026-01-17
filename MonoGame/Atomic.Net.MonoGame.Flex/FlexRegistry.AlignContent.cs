using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<AlignContentBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<AlignContentBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<AlignContentBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<AlignContentBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<AlignContentBehavior>(out var alignContent))
        {
            _nodes[e.Entity.Index]!.StyleSetAlignContent(alignContent.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<AlignContentBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<AlignContentBehavior>(out var alignContent))
        {
            _nodes[e.Entity.Index]!.StyleSetAlignContent(alignContent.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<AlignContentBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetAlignContent(default);
    }
}
