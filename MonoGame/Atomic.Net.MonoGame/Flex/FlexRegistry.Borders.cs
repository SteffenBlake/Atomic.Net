using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using FlexLayoutSharp;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    // Borders
    IEventHandler<BehaviorAddedEvent<FlexBorderLeftBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexBorderLeftBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexBorderLeftBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexBorderRightBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexBorderRightBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexBorderRightBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexBorderTopBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexBorderTopBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexBorderTopBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexBorderBottomBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexBorderBottomBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexBorderBottomBehavior>>
{
    // BorderLeft
    public void OnEvent(BehaviorAddedEvent<FlexBorderLeftBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexBorderLeftBehavior>(out var val))
        {
            if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
            {
                node!.StyleSetBorder(Edge.Left, val.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexBorderLeftBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexBorderLeftBehavior>(out var val))
        {
            if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
            {
                node!.StyleSetBorder(Edge.Left, val.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexBorderLeftBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
        {
            node!.StyleSetBorder(Edge.Left, float.NaN);
        }
    }

    // BorderRight
    public void OnEvent(BehaviorAddedEvent<FlexBorderRightBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexBorderRightBehavior>(out var val))
        {
            if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
            {
                node!.StyleSetBorder(Edge.Right, val.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexBorderRightBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexBorderRightBehavior>(out var val))
        {
            if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
            {
                node!.StyleSetBorder(Edge.Right, val.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexBorderRightBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
        {
            node!.StyleSetBorder(Edge.Right, float.NaN);
        }
    }

    // BorderTop
    public void OnEvent(BehaviorAddedEvent<FlexBorderTopBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexBorderTopBehavior>(out var val))
        {
            if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
            {
                node!.StyleSetBorder(Edge.Top, val.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexBorderTopBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexBorderTopBehavior>(out var val))
        {
            if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
            {
                node!.StyleSetBorder(Edge.Top, val.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexBorderTopBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
        {
            node!.StyleSetBorder(Edge.Top, float.NaN);
        }
    }

    // BorderBottom
    public void OnEvent(BehaviorAddedEvent<FlexBorderBottomBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexBorderBottomBehavior>(out var val))
        {
            if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
            {
                node!.StyleSetBorder(Edge.Bottom, val.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexBorderBottomBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexBorderBottomBehavior>(out var val))
        {
            if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
            {
                node!.StyleSetBorder(Edge.Bottom, val.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexBorderBottomBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
        {
            node!.StyleSetBorder(Edge.Bottom, float.NaN);
        }
    }
}
