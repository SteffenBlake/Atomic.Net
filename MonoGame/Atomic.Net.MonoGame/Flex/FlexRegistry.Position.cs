using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using FlexLayoutSharp;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry :
    IEventHandler<BehaviorAddedEvent<FlexPositionTypeBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexPositionTypeBehavior>>,
    IEventHandler<PostBehaviorRemovedEvent<FlexPositionTypeBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexPositionLeftBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexPositionLeftBehavior>>,
    IEventHandler<PostBehaviorRemovedEvent<FlexPositionLeftBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexPositionRightBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexPositionRightBehavior>>,
    IEventHandler<PostBehaviorRemovedEvent<FlexPositionRightBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexPositionTopBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexPositionTopBehavior>>,
    IEventHandler<PostBehaviorRemovedEvent<FlexPositionTopBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexPositionBottomBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexPositionBottomBehavior>>,
    IEventHandler<PostBehaviorRemovedEvent<FlexPositionBottomBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<FlexPositionTypeBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPositionTypeBehavior>(out var posType))
        {
            node.StyleSetPositionType(posType.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexPositionTypeBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPositionTypeBehavior>(out var posType))
        {
            node.StyleSetPositionType(posType.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorRemovedEvent<FlexPositionTypeBehavior> e)
    {
        SetDirtyNode(e.Entity.Index);
    }
    public void OnEvent(BehaviorAddedEvent<FlexPositionLeftBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPositionLeftBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                node.StyleSetPositionPercent(Edge.Left, pos.Value.Value);
            }
            else
            {
                node.StyleSetPosition(Edge.Left, pos.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexPositionLeftBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPositionLeftBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                node.StyleSetPositionPercent(Edge.Left, pos.Value.Value);
            }
            else
            {
                node.StyleSetPosition(Edge.Left, pos.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorRemovedEvent<FlexPositionLeftBehavior> e)
    {
        SetDirtyNode(e.Entity.Index);
    }

    public void OnEvent(BehaviorAddedEvent<FlexPositionRightBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPositionRightBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                node.StyleSetPositionPercent(Edge.Right, pos.Value.Value);
            }
            else
            {
                node.StyleSetPosition(Edge.Right, pos.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexPositionRightBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPositionRightBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                node.StyleSetPositionPercent(Edge.Right, pos.Value.Value);
            }
            else
            {
                node.StyleSetPosition(Edge.Right, pos.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorRemovedEvent<FlexPositionRightBehavior> e)
    {
        SetDirtyNode(e.Entity.Index);
    }

    public void OnEvent(BehaviorAddedEvent<FlexPositionTopBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPositionTopBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                node.StyleSetPositionPercent(Edge.Top, pos.Value.Value);
            }
            else
            {
                node.StyleSetPosition(Edge.Top, pos.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexPositionTopBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPositionTopBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                node.StyleSetPositionPercent(Edge.Top, pos.Value.Value);
            }
            else
            {
                node.StyleSetPosition(Edge.Top, pos.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorRemovedEvent<FlexPositionTopBehavior> e)
    {
        SetDirtyNode(e.Entity.Index);
    }

    public void OnEvent(BehaviorAddedEvent<FlexPositionBottomBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPositionBottomBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                node.StyleSetPositionPercent(Edge.Bottom, pos.Value.Value);
            }
            else
            {
                node.StyleSetPosition(Edge.Bottom, pos.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexPositionBottomBehavior> e)
    {
        var node = EnsureDirtyNode(e.Entity.Index);
        if (e.Entity.TryGetBehavior<FlexPositionBottomBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                node.StyleSetPositionPercent(Edge.Bottom, pos.Value.Value);
            }
            else
            {
                node.StyleSetPosition(Edge.Bottom, pos.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorRemovedEvent<FlexPositionBottomBehavior> e)
    {
        SetDirtyNode(e.Entity.Index);
    }
}
