using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using FlexLayoutSharp;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    // Margins
    IEventHandler<BehaviorAddedEvent<MarginLeftBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<MarginLeftBehavior>>,
    IEventHandler<BehaviorRemovedEvent<MarginLeftBehavior>>,

    IEventHandler<BehaviorAddedEvent<MarginRightBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<MarginRightBehavior>>,
    IEventHandler<BehaviorRemovedEvent<MarginRightBehavior>>,

    IEventHandler<BehaviorAddedEvent<MarginTopBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<MarginTopBehavior>>,
    IEventHandler<BehaviorRemovedEvent<MarginTopBehavior>>,

    IEventHandler<BehaviorAddedEvent<MarginBottomBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<MarginBottomBehavior>>,
    IEventHandler<BehaviorRemovedEvent<MarginBottomBehavior>>
{
    // MarginLeft
    public void OnEvent(BehaviorAddedEvent<MarginLeftBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<MarginLeftBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Left, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<MarginLeftBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<MarginLeftBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Left, val.Value.Value);
        }
    }

    public void OnEvent(BehaviorRemovedEvent<MarginLeftBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Left, float.NaN);
    }

    // MarginRight
    public void OnEvent(BehaviorAddedEvent<MarginRightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<MarginRightBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Right, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<MarginRightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<MarginRightBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Right, val.Value.Value);
        }
    }

    public void OnEvent(BehaviorRemovedEvent<MarginRightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Right, float.NaN);
    }

    // MarginTop
    public void OnEvent(BehaviorAddedEvent<MarginTopBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<MarginTopBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Top, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<MarginTopBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<MarginTopBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Top, val.Value.Value);
        }
    }

    public void OnEvent(BehaviorRemovedEvent<MarginTopBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Top, float.NaN);
    }

    // MarginBottom
    public void OnEvent(BehaviorAddedEvent<MarginBottomBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<MarginBottomBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Bottom, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<MarginBottomBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<MarginBottomBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Bottom, val.Value.Value);
        }
    }

    public void OnEvent(BehaviorRemovedEvent<MarginBottomBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetMargin(Edge.Bottom, float.NaN);
    }
}
