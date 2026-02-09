using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using FlexLayoutSharp;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry :
    // Margins
    IEventHandler<BehaviorAddedEvent<FlexMarginLeftBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexMarginLeftBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexMarginLeftBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexMarginRightBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexMarginRightBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexMarginRightBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexMarginTopBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexMarginTopBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexMarginTopBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexMarginBottomBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexMarginBottomBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexMarginBottomBehavior>>
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

    public void OnEvent(PreBehaviorRemovedEvent<FlexMarginLeftBehavior> e)
    {
        // If entity no longer has FlexBehavior, skip - the FlexBehavior removal handler will clean up
        if (!e.Entity.HasBehavior<FlexBehavior>())
        {
            return;
        }

        var node = EnsureDirtyNode(e.Entity.Index);
        node.StyleSetMargin(Edge.Left, float.NaN);
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

    public void OnEvent(PreBehaviorRemovedEvent<FlexMarginRightBehavior> e)
    {
        // If entity no longer has FlexBehavior, skip - the FlexBehavior removal handler will clean up
        if (!e.Entity.HasBehavior<FlexBehavior>())
        {
            return;
        }

        var node = EnsureDirtyNode(e.Entity.Index);
        node.StyleSetMargin(Edge.Right, float.NaN);
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

    public void OnEvent(PreBehaviorRemovedEvent<FlexMarginTopBehavior> e)
    {
        // If entity no longer has FlexBehavior, skip - the FlexBehavior removal handler will clean up
        if (!e.Entity.HasBehavior<FlexBehavior>())
        {
            return;
        }

        var node = EnsureDirtyNode(e.Entity.Index);
        node.StyleSetMargin(Edge.Top, float.NaN);
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

    public void OnEvent(PreBehaviorRemovedEvent<FlexMarginBottomBehavior> e)
    {
        // If entity no longer has FlexBehavior, skip - the FlexBehavior removal handler will clean up
        if (!e.Entity.HasBehavior<FlexBehavior>())
        {
            return;
        }

        var node = EnsureDirtyNode(e.Entity.Index);
        node.StyleSetMargin(Edge.Bottom, float.NaN);
    }
}
