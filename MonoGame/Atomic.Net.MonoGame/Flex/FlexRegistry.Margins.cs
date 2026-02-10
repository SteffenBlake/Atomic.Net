using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using FlexLayoutSharp;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry :
    // Margins
    IEventHandler<BehaviorAddedEvent<FlexMarginLeftBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexMarginLeftBehavior>>,
    IEventHandler<PostBehaviorRemovedEvent<FlexMarginLeftBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexMarginRightBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexMarginRightBehavior>>,
    IEventHandler<PostBehaviorRemovedEvent<FlexMarginRightBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexMarginTopBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexMarginTopBehavior>>,
    IEventHandler<PostBehaviorRemovedEvent<FlexMarginTopBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexMarginBottomBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexMarginBottomBehavior>>,
    IEventHandler<PostBehaviorRemovedEvent<FlexMarginBottomBehavior>>
{
    // MarginLeft
    public void OnEvent(BehaviorAddedEvent<FlexMarginLeftBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexMarginLeftBehavior>(out var val))
        {
            node.StyleSetMargin(Edge.Left, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexMarginLeftBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexMarginLeftBehavior>(out var val))
        {
            node.StyleSetMargin(Edge.Left, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorRemovedEvent<FlexMarginLeftBehavior> e)
    {
        SetDirtyNode(e.Entity.Index);
    }

    // MarginRight
    public void OnEvent(BehaviorAddedEvent<FlexMarginRightBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexMarginRightBehavior>(out var val))
        {
            node.StyleSetMargin(Edge.Right, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexMarginRightBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexMarginRightBehavior>(out var val))
        {
            node.StyleSetMargin(Edge.Right, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorRemovedEvent<FlexMarginRightBehavior> e)
    {
        SetDirtyNode(e.Entity.Index);
    }

    // MarginTop
    public void OnEvent(BehaviorAddedEvent<FlexMarginTopBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexMarginTopBehavior>(out var val))
        {
            node.StyleSetMargin(Edge.Top, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexMarginTopBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexMarginTopBehavior>(out var val))
        {
            node.StyleSetMargin(Edge.Top, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorRemovedEvent<FlexMarginTopBehavior> e)
    {
        SetDirtyNode(e.Entity.Index);
    }

    // MarginBottom
    public void OnEvent(BehaviorAddedEvent<FlexMarginBottomBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexMarginBottomBehavior>(out var val))
        {
            node.StyleSetMargin(Edge.Bottom, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexMarginBottomBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexMarginBottomBehavior>(out var val))
        {
            node.StyleSetMargin(Edge.Bottom, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorRemovedEvent<FlexMarginBottomBehavior> e)
    {
        SetDirtyNode(e.Entity.Index);
    }
}
