using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<AlignItemsBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<AlignItemsBehavior>>,
    IEventHandler<BehaviorRemovedEvent<AlignItemsBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<AlignItemsBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<AlignItemsBehavior>(out var align))
        {
            _nodes[e.Entity.Index]!.StyleSetAlignItems(align.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<AlignItemsBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<AlignItemsBehavior>(out var align))
        {
            _nodes[e.Entity.Index]!.StyleSetAlignItems(align.Value.Value);
        }
    }

    public void OnEvent(BehaviorRemovedEvent<AlignItemsBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetAlignItems(default);
    }
}
