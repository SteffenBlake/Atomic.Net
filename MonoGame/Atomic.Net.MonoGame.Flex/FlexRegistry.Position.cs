using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using FlexLayoutSharp;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    IEventHandler<BehaviorAddedEvent<PositionTypeBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<PositionTypeBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<PositionTypeBehavior>>,

    IEventHandler<BehaviorAddedEvent<PositionLeftBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<PositionLeftBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<PositionLeftBehavior>>,

    IEventHandler<BehaviorAddedEvent<PositionRightBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<PositionRightBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<PositionRightBehavior>>,

    IEventHandler<BehaviorAddedEvent<PositionTopBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<PositionTopBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<PositionTopBehavior>>,

    IEventHandler<BehaviorAddedEvent<PositionBottomBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<PositionBottomBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<PositionBottomBehavior>>
{
    public void OnEvent(BehaviorAddedEvent<PositionTypeBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PositionTypeBehavior>(out var posType))
        {
            _nodes[e.Entity.Index]!.StyleSetPositionType(posType.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<PositionTypeBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PositionTypeBehavior>(out var posType))
        {
            _nodes[e.Entity.Index]!.StyleSetPositionType(posType.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<PositionTypeBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetPositionType(default);
    }
    public void OnEvent(BehaviorAddedEvent<PositionLeftBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PositionLeftBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                _nodes[e.Entity.Index]!.StyleSetPositionPercent(Edge.Left, pos.Value.Value);
            }
            else
            {
                _nodes[e.Entity.Index]!.StyleSetPosition(Edge.Left, pos.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<PositionLeftBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PositionLeftBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                _nodes[e.Entity.Index]!.StyleSetPositionPercent(Edge.Left, pos.Value.Value);
            }
            else
            {
                _nodes[e.Entity.Index]!.StyleSetPosition(Edge.Left, pos.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<PositionLeftBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetPosition(Edge.Left, float.NaN);
    }

    public void OnEvent(BehaviorAddedEvent<PositionRightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PositionRightBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                _nodes[e.Entity.Index]!.StyleSetPositionPercent(Edge.Right, pos.Value.Value);
            }
            else
            {
                _nodes[e.Entity.Index]!.StyleSetPosition(Edge.Right, pos.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<PositionRightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PositionRightBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                _nodes[e.Entity.Index]!.StyleSetPositionPercent(Edge.Right, pos.Value.Value);
            }
            else
            {
                _nodes[e.Entity.Index]!.StyleSetPosition(Edge.Right, pos.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<PositionRightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetPosition(Edge.Right, float.NaN);
    }

    public void OnEvent(BehaviorAddedEvent<PositionTopBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PositionTopBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                _nodes[e.Entity.Index]!.StyleSetPositionPercent(Edge.Top, pos.Value.Value);
            }
            else
            {
                _nodes[e.Entity.Index]!.StyleSetPosition(Edge.Top, pos.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<PositionTopBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PositionTopBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                _nodes[e.Entity.Index]!.StyleSetPositionPercent(Edge.Top, pos.Value.Value);
            }
            else
            {
                _nodes[e.Entity.Index]!.StyleSetPosition(Edge.Top, pos.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<PositionTopBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetPosition(Edge.Top, float.NaN);
    }

    public void OnEvent(BehaviorAddedEvent<PositionBottomBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PositionBottomBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                _nodes[e.Entity.Index]!.StyleSetPositionPercent(Edge.Bottom, pos.Value.Value);
            }
            else
            {
                _nodes[e.Entity.Index]!.StyleSetPosition(Edge.Bottom, pos.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<PositionBottomBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PositionBottomBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                _nodes[e.Entity.Index]!.StyleSetPositionPercent(Edge.Bottom, pos.Value.Value);
            }
            else
            {
                _nodes[e.Entity.Index]!.StyleSetPosition(Edge.Bottom, pos.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<PositionBottomBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetPosition(Edge.Bottom, float.NaN);
    }
}
