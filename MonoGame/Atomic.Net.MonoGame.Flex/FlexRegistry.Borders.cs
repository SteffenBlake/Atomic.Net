using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using FlexLayoutSharp;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    // Borders
    IEventHandler<BehaviorAddedEvent<BorderLeftBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<BorderLeftBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<BorderLeftBehavior>>,

    IEventHandler<BehaviorAddedEvent<BorderRightBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<BorderRightBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<BorderRightBehavior>>,

    IEventHandler<BehaviorAddedEvent<BorderTopBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<BorderTopBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<BorderTopBehavior>>,

    IEventHandler<BehaviorAddedEvent<BorderBottomBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<BorderBottomBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<BorderBottomBehavior>>
{
    // BorderLeft
    public void OnEvent(BehaviorAddedEvent<BorderLeftBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<BorderLeftBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetBorder(Edge.Left, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<BorderLeftBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<BorderLeftBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetBorder(Edge.Left, val.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<BorderLeftBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetBorder(Edge.Left, float.NaN);
    }

    // BorderRight
    public void OnEvent(BehaviorAddedEvent<BorderRightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<BorderRightBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetBorder(Edge.Right, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<BorderRightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<BorderRightBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetBorder(Edge.Right, val.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<BorderRightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetBorder(Edge.Right, float.NaN);
    }

    // BorderTop
    public void OnEvent(BehaviorAddedEvent<BorderTopBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<BorderTopBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetBorder(Edge.Top, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<BorderTopBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<BorderTopBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetBorder(Edge.Top, val.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<BorderTopBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetBorder(Edge.Top, float.NaN);
    }

    // BorderBottom
    public void OnEvent(BehaviorAddedEvent<BorderBottomBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<BorderBottomBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetBorder(Edge.Bottom, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<BorderBottomBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<BorderBottomBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetBorder(Edge.Bottom, val.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<BorderBottomBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetBorder(Edge.Bottom, float.NaN);
    }
}
