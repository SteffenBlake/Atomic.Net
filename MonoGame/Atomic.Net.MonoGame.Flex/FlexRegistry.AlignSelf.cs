using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<AlignSelfBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<AlignSelfBehavior>>,
    IEventHandler<BehaviorRemovedEvent<AlignSelfBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<AlignSelfBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<AlignSelfBehavior>(out var alignSelf))
        {
            _nodes[e.Entity.Index]!.StyleSetAlignSelf(alignSelf.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<AlignSelfBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<AlignSelfBehavior>(out var alignSelf))
        {
            _nodes[e.Entity.Index]!.StyleSetAlignSelf(alignSelf.Value.Value);
        }
    }

    public void OnEvent(BehaviorRemovedEvent<AlignSelfBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetAlignSelf(default);
    }
}
