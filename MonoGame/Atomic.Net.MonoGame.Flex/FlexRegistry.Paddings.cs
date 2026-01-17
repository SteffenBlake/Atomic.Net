using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using FlexLayoutSharp;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    // Padding
    IEventHandler<BehaviorAddedEvent<PaddingLeftBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<PaddingLeftBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<PaddingLeftBehavior>>,

    IEventHandler<BehaviorAddedEvent<PaddingRightBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<PaddingRightBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<PaddingRightBehavior>>,

    IEventHandler<BehaviorAddedEvent<PaddingTopBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<PaddingTopBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<PaddingTopBehavior>>,

    IEventHandler<BehaviorAddedEvent<PaddingBottomBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<PaddingBottomBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<PaddingBottomBehavior>>
{
    // PaddingLeft
    public void OnEvent(BehaviorAddedEvent<PaddingLeftBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PaddingLeftBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetPadding(Edge.Left, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<PaddingLeftBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PaddingLeftBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetPadding(Edge.Left, val.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<PaddingLeftBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetPadding(Edge.Left, float.NaN);
    }

    // PaddingRight
    public void OnEvent(BehaviorAddedEvent<PaddingRightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PaddingRightBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetPadding(Edge.Right, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<PaddingRightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PaddingRightBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetPadding(Edge.Right, val.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<PaddingRightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetPadding(Edge.Right, float.NaN);
    }

    // PaddingTop
    public void OnEvent(BehaviorAddedEvent<PaddingTopBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PaddingTopBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetPadding(Edge.Top, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<PaddingTopBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PaddingTopBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetPadding(Edge.Top, val.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<PaddingTopBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetPadding(Edge.Top, float.NaN);
    }

    // PaddingBottom
    public void OnEvent(BehaviorAddedEvent<PaddingBottomBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PaddingBottomBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetPadding(Edge.Bottom, val.Value.Value);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<PaddingBottomBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<PaddingBottomBehavior>(out var val))
        {
            _nodes[e.Entity.Index]!.StyleSetPadding(Edge.Bottom, val.Value.Value);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<PaddingBottomBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetPadding(Edge.Bottom, float.NaN);
    }
}
