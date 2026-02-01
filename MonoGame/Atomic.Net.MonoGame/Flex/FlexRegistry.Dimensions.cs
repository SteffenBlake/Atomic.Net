using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    // Width / Height
    IEventHandler<BehaviorAddedEvent<FlexWidthBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexWidthBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexWidthBehavior>>,

    IEventHandler<BehaviorAddedEvent<FlexHeightBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexHeightBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexHeightBehavior>>
{
    // Width
    public void OnEvent(BehaviorAddedEvent<FlexWidthBehavior> e)
    {
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexWidthBehavior>(out var val))
        {
            if (val.Value.Percent)
            {
                _nodes[e.Entity.Index.ToInt()]!.StyleSetWidthPercent(val.Value.Value);
            }
            else
            {
                _nodes[e.Entity.Index.ToInt()]!.StyleSetWidth(val.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexWidthBehavior> e)
    {
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexWidthBehavior>(out var val))
        {
            if (val.Value.Percent)
            {
                _nodes[e.Entity.Index.ToInt()]!.StyleSetWidthPercent(val.Value.Value);
            }
            else
            {
                _nodes[e.Entity.Index.ToInt()]!.StyleSetWidth(val.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexWidthBehavior> e)
    {
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index.ToInt()]!.StyleSetWidth(float.NaN);
    }

    // Height
    public void OnEvent(BehaviorAddedEvent<FlexHeightBehavior> e)
    {
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexHeightBehavior>(out var val))
        {
            if (val.Value.Percent)
            {
                _nodes[e.Entity.Index.ToInt()]!.StyleSetHeightPercent(val.Value.Value);
            }
            else
            {
                _nodes[e.Entity.Index.ToInt()]!.StyleSetHeight(val.Value.Value);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexHeightBehavior> e)
    {
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        if (e.Entity.TryGetBehavior<FlexHeightBehavior>(out var val))
        {
            if (val.Value.Percent)
            {
                _nodes[e.Entity.Index.ToInt()]!.StyleSetHeightPercent(val.Value.Value);
            }
            else
            {
                _nodes[e.Entity.Index.ToInt()]!.StyleSetHeight(val.Value.Value);
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexHeightBehavior> e)
    {
        _dirty[e.Entity.Index.ToInt()] = true;
        _nodes[e.Entity.Index.ToInt()] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _nodes[e.Entity.Index.ToInt()]!.StyleSetHeight(float.NaN);
    }
}
