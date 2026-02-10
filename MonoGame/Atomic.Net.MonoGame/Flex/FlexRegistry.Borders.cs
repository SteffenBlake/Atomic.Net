using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using FlexLayoutSharp;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry :
    // Borders
    IEventHandler<BehaviorAddedEvent<FlexBorderLeftBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexBorderLeftBehavior>>,
    IEventHandler<PostBehaviorRemovedEvent<FlexBorderLeftBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexBorderRightBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexBorderRightBehavior>>,
    IEventHandler<PostBehaviorRemovedEvent<FlexBorderRightBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexBorderTopBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexBorderTopBehavior>>,
    IEventHandler<PostBehaviorRemovedEvent<FlexBorderTopBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexBorderBottomBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexBorderBottomBehavior>>,
    IEventHandler<PostBehaviorRemovedEvent<FlexBorderBottomBehavior>>
{
    // BorderLeft
    public void OnEvent(BehaviorAddedEvent<FlexBorderLeftBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexBorderLeftBehavior>(out var val))
        {
            node.StyleSetBorder(Edge.Left, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexBorderLeftBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexBorderLeftBehavior>(out var val))
        {
            node.StyleSetBorder(Edge.Left, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorRemovedEvent<FlexBorderLeftBehavior> e)
    {
        SetDirtyNode(e.Entity.Index);
    }

    // BorderRight
    public void OnEvent(BehaviorAddedEvent<FlexBorderRightBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexBorderRightBehavior>(out var val))
        {
            node.StyleSetBorder(Edge.Right, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexBorderRightBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexBorderRightBehavior>(out var val))
        {
            node.StyleSetBorder(Edge.Right, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorRemovedEvent<FlexBorderRightBehavior> e)
    {
        SetDirtyNode(e.Entity.Index);
    }

    // BorderTop
    public void OnEvent(BehaviorAddedEvent<FlexBorderTopBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexBorderTopBehavior>(out var val))
        {
            node.StyleSetBorder(Edge.Top, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexBorderTopBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexBorderTopBehavior>(out var val))
        {
            node.StyleSetBorder(Edge.Top, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorRemovedEvent<FlexBorderTopBehavior> e)
    {
        SetDirtyNode(e.Entity.Index);
    }

    // BorderBottom
    public void OnEvent(BehaviorAddedEvent<FlexBorderBottomBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexBorderBottomBehavior>(out var val))
        {
            node.StyleSetBorder(Edge.Bottom, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexBorderBottomBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexBorderBottomBehavior>(out var val))
        {
            node.StyleSetBorder(Edge.Bottom, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorRemovedEvent<FlexBorderBottomBehavior> e)
    {
        SetDirtyNode(e.Entity.Index);
    }
}
