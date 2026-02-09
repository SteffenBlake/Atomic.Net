using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using FlexLayoutSharp;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry :
    IEventHandler<BehaviorAddedEvent<FlexPositionTypeBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexPositionTypeBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexPositionTypeBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexPositionLeftBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexPositionLeftBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexPositionLeftBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexPositionRightBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexPositionRightBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexPositionRightBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexPositionTopBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexPositionTopBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexPositionTopBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexPositionBottomBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexPositionBottomBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexPositionBottomBehavior>>
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

    public void OnEvent(PreBehaviorRemovedEvent<FlexPositionTypeBehavior> e)
    {
        // If entity no longer has FlexBehavior, skip - the FlexBehavior removal handler will clean up
        if (!e.Entity.HasBehavior<FlexBehavior>())
        {
            return;
        }

        var node = EnsureDirtyNode(e.Entity.Index);
        node.StyleSetPositionType(default);
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

    public void OnEvent(PreBehaviorRemovedEvent<FlexPositionLeftBehavior> e)
    {
        // If entity no longer has FlexBehavior, skip - the FlexBehavior removal handler will clean up
        if (!e.Entity.HasBehavior<FlexBehavior>())
        {
            return;
        }

        var node = EnsureDirtyNode(e.Entity.Index);
        node.StyleSetPosition(Edge.Left, float.NaN);
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

    public void OnEvent(PreBehaviorRemovedEvent<FlexPositionRightBehavior> e)
    {
        // If entity no longer has FlexBehavior, skip - the FlexBehavior removal handler will clean up
        if (!e.Entity.HasBehavior<FlexBehavior>())
        {
            return;
        }

        var node = EnsureDirtyNode(e.Entity.Index);
        node.StyleSetPosition(Edge.Right, float.NaN);
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

    public void OnEvent(PreBehaviorRemovedEvent<FlexPositionTopBehavior> e)
    {
        // If entity no longer has FlexBehavior, skip - the FlexBehavior removal handler will clean up
        if (!e.Entity.HasBehavior<FlexBehavior>())
        {
            return;
        }

        var node = EnsureDirtyNode(e.Entity.Index);
        node.StyleSetPosition(Edge.Top, float.NaN);
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

    public void OnEvent(PreBehaviorRemovedEvent<FlexPositionBottomBehavior> e)
    {
        // If entity no longer has FlexBehavior, skip - the FlexBehavior removal handler will clean up
        if (!e.Entity.HasBehavior<FlexBehavior>())
        {
            return;
        }

        var node = EnsureDirtyNode(e.Entity.Index);
        node.StyleSetPosition(Edge.Bottom, float.NaN);
    }
}
