using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    // Width / Height
    IEventHandler<BehaviorAddedEvent<WidthBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<WidthBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<WidthBehavior>>,

    IEventHandler<BehaviorAddedEvent<HeightBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<HeightBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<HeightBehavior>>
{
    // Width
    public void OnEvent(BehaviorAddedEvent<WidthBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<WidthBehavior>(out var val))
        {
            if (val.Value.Percent)
            {
                _nodes[e.Entity.Index]!.StyleSetWidthPercent(val.Value.Value);
            }
            else
            {
                _nodes[e.Entity.Index]!.StyleSetWidth(val.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<WidthBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<WidthBehavior>(out var val))
        {
            if (val.Value.Percent)
            {
                _nodes[e.Entity.Index]!.StyleSetWidthPercent(val.Value.Value);
            }
            else
            {
                _nodes[e.Entity.Index]!.StyleSetWidth(val.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<WidthBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetWidth(float.NaN);
    }

    // Height
    public void OnEvent(BehaviorAddedEvent<HeightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<HeightBehavior>(out var val))
        {
            if (val.Value.Percent)
            {
                _nodes[e.Entity.Index]!.StyleSetHeightPercent(val.Value.Value);
            }
            else
            {
                _nodes[e.Entity.Index]!.StyleSetHeight(val.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<HeightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<HeightBehavior>(out var val))
        {
            if (val.Value.Percent)
            {
                _nodes[e.Entity.Index]!.StyleSetHeightPercent(val.Value.Value);
            }
            else
            {
                _nodes[e.Entity.Index]!.StyleSetHeight(val.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<HeightBehavior> e)
    {
        _dirty[e.Entity.Index] = true;
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index]!.StyleSetHeight(float.NaN);
    }
}
