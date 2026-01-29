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
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexMarginLeftBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Left, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexMarginLeftBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexMarginLeftBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Left, val.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexMarginLeftBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Left, float.NaN);
    }

    // MarginRight
    public void OnEvent(BehaviorAddedEvent<FlexMarginRightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexMarginRightBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Right, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexMarginRightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexMarginRightBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Right, val.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexMarginRightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Right, float.NaN);
    }

    // MarginTop
    public void OnEvent(BehaviorAddedEvent<FlexMarginTopBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexMarginTopBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Top, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexMarginTopBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexMarginTopBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Top, val.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexMarginTopBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Top, float.NaN);
    }

    // MarginBottom
    public void OnEvent(BehaviorAddedEvent<FlexMarginBottomBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexMarginBottomBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Bottom, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexMarginBottomBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexMarginBottomBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Bottom, val.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexMarginBottomBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Bottom, float.NaN);
    }
}
