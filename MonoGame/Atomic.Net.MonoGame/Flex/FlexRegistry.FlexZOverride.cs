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
        _ = EnsureDirtyNode(e.Entity.Index);
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexZOverride> e)
    {
        _ = EnsureDirtyNode(e.Entity.Index);
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexZOverride> e)
    {
        _ = EnsureDirtyNode(e.Entity.Index);
    }
}
