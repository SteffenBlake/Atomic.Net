using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<FlexAlignSelfBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexAlignSelfBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexAlignSelfBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexAlignSelfBehavior> e)
    {
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexAlignSelfBehavior>(out var alignSelf))
        {
            _nodes[e.Entity.Index.ToInt()]!.StyleSetAlignSelf(alignSelf.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexAlignSelfBehavior> e)
    {
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexAlignSelfBehavior>(out var alignSelf))
        {
            _nodes[e.Entity.Index.ToInt()]!.StyleSetAlignSelf(alignSelf.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexAlignSelfBehavior> e)
    {
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index.ToInt()]!.StyleSetAlignSelf(default);
    }
}
