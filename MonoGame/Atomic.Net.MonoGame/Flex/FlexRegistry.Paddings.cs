using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using FlexLayoutSharp;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry :
    // Padding
    IEventHandler<BehaviorAddedEvent<FlexPaddingLeftBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexPaddingLeftBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexPaddingLeftBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexPaddingRightBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexPaddingRightBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexPaddingRightBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexPaddingTopBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexPaddingTopBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexPaddingTopBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexPaddingBottomBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexPaddingBottomBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexPaddingBottomBehavior>>
{
    // PaddingLeft
    public void OnEvent(BehaviorAddedEvent<FlexPaddingLeftBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPaddingLeftBehavior>(out var val))
        {
            node.StyleSetPadding(Edge.Left, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexPaddingLeftBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPaddingLeftBehavior>(out var val))
        {
            node.StyleSetPadding(Edge.Left, val.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexPaddingLeftBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        node.StyleSetPadding(Edge.Left, float.NaN);
    }

    // PaddingRight
    public void OnEvent(BehaviorAddedEvent<FlexPaddingRightBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPaddingRightBehavior>(out var val))
        {
            node.StyleSetPadding(Edge.Right, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexPaddingRightBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPaddingRightBehavior>(out var val))
        {
            node.StyleSetPadding(Edge.Right, val.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexPaddingRightBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        node.StyleSetPadding(Edge.Right, float.NaN);
    }

    // PaddingTop
    public void OnEvent(BehaviorAddedEvent<FlexPaddingTopBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPaddingTopBehavior>(out var val))
        {
            node.StyleSetPadding(Edge.Top, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexPaddingTopBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPaddingTopBehavior>(out var val))
        {
            node.StyleSetPadding(Edge.Top, val.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexPaddingTopBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        node.StyleSetPadding(Edge.Top, float.NaN);
    }

    // PaddingBottom
    public void OnEvent(BehaviorAddedEvent<FlexPaddingBottomBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPaddingBottomBehavior>(out var val))
        {
            node.StyleSetPadding(Edge.Bottom, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexPaddingBottomBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPaddingBottomBehavior>(out var val))
        {
            node.StyleSetPadding(Edge.Bottom, val.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexPaddingBottomBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        node.StyleSetPadding(Edge.Bottom, float.NaN);
    }
}
