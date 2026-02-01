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
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index))
        {
            _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexPositionTypeBehavior>(out var posType))
        {
            if (_nodes.TryGetValue(e.Entity.Index, out var node))
            {
                node!.StyleSetPositionType(posType.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexPositionTypeBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index))
        {
            _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexPositionTypeBehavior>(out var posType))
        {
            if (_nodes.TryGetValue(e.Entity.Index, out var node))
            {
                node!.StyleSetPositionType(posType.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexPositionTypeBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index))
        {
            _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node!.StyleSetPositionType(default);
        }
    }
    public void OnEvent(BehaviorAddedEvent<FlexPositionLeftBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index))
        {
            _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexPositionLeftBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                if (_nodes.TryGetValue(e.Entity.Index, out var node))
                {
                    node!.StyleSetPositionPercent(Edge.Left, pos.Value.Value);
                }
            }
            else
            {
                if (_nodes.TryGetValue(e.Entity.Index, out var node))
                {
                    node!.StyleSetPosition(Edge.Left, pos.Value.Value);
                }
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexPositionLeftBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index))
        {
            _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexPositionLeftBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                if (_nodes.TryGetValue(e.Entity.Index, out var node))
                {
                    node!.StyleSetPositionPercent(Edge.Left, pos.Value.Value);
                }
            }
            else
            {
                if (_nodes.TryGetValue(e.Entity.Index, out var node))
                {
                    node!.StyleSetPosition(Edge.Left, pos.Value.Value);
                }
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexPositionLeftBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index))
        {
            _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node!.StyleSetPosition(Edge.Left, float.NaN);
        }
    }

    public void OnEvent(BehaviorAddedEvent<FlexPositionRightBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index))
        {
            _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexPositionRightBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                if (_nodes.TryGetValue(e.Entity.Index, out var node))
                {
                    node!.StyleSetPositionPercent(Edge.Right, pos.Value.Value);
                }
            }
            else
            {
                if (_nodes.TryGetValue(e.Entity.Index, out var node))
                {
                    node!.StyleSetPosition(Edge.Right, pos.Value.Value);
                }
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexPositionRightBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index))
        {
            _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexPositionRightBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                if (_nodes.TryGetValue(e.Entity.Index, out var node))
                {
                    node!.StyleSetPositionPercent(Edge.Right, pos.Value.Value);
                }
            }
            else
            {
                if (_nodes.TryGetValue(e.Entity.Index, out var node))
                {
                    node!.StyleSetPosition(Edge.Right, pos.Value.Value);
                }
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexPositionRightBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index))
        {
            _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node!.StyleSetPosition(Edge.Right, float.NaN);
        }
    }

    public void OnEvent(BehaviorAddedEvent<FlexPositionTopBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index))
        {
            _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexPositionTopBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                if (_nodes.TryGetValue(e.Entity.Index, out var node))
                {
                    node!.StyleSetPositionPercent(Edge.Top, pos.Value.Value);
                }
            }
            else
            {
                if (_nodes.TryGetValue(e.Entity.Index, out var node))
                {
                    node!.StyleSetPosition(Edge.Top, pos.Value.Value);
                }
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexPositionTopBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index))
        {
            _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexPositionTopBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                if (_nodes.TryGetValue(e.Entity.Index, out var node))
                {
                    node!.StyleSetPositionPercent(Edge.Top, pos.Value.Value);
                }
            }
            else
            {
                if (_nodes.TryGetValue(e.Entity.Index, out var node))
                {
                    node!.StyleSetPosition(Edge.Top, pos.Value.Value);
                }
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexPositionTopBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index))
        {
            _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node!.StyleSetPosition(Edge.Top, float.NaN);
        }
    }

    public void OnEvent(BehaviorAddedEvent<FlexPositionBottomBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index))
        {
            _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexPositionBottomBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                if (_nodes.TryGetValue(e.Entity.Index, out var node))
                {
                    node!.StyleSetPositionPercent(Edge.Bottom, pos.Value.Value);
                }
            }
            else
            {
                if (_nodes.TryGetValue(e.Entity.Index, out var node))
                {
                    node!.StyleSetPosition(Edge.Bottom, pos.Value.Value);
                }
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexPositionBottomBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index))
        {
            _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexPositionBottomBehavior>(out var pos))
        {
            if (pos.Value.Percent)
            {
                if (_nodes.TryGetValue(e.Entity.Index, out var node))
                {
                    node!.StyleSetPositionPercent(Edge.Bottom, pos.Value.Value);
                }
            }
            else
            {
                if (_nodes.TryGetValue(e.Entity.Index, out var node))
                {
                    node!.StyleSetPosition(Edge.Bottom, pos.Value.Value);
                }
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexPositionBottomBehavior> e)
    {
        _dirty.Set(e.Entity.Index, true);
        if (!_nodes.HasValue(e.Entity.Index))
        {
            _nodes[e.Entity.Index] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            node!.StyleSetPosition(Edge.Bottom, float.NaN);
        }
    }
}
