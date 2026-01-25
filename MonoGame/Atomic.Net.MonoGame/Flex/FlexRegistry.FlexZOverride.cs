using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<FlexZOverride>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexZOverride>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexZOverride>>
{
    public void OnEvent(BehaviorAddedEvent<FlexZOverride> e)
    {
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _dirty[e.Entity.Index] = true;
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexZOverride> e)
    {
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _dirty[e.Entity.Index] = true;
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexZOverride> e)
    {
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _dirty[e.Entity.Index] = true;
    }
}
