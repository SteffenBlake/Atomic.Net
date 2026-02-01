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
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexWidthBehavior>(out var val))
        {
            if (val.Value.Percent)
            {
                if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
                {
                    node!.StyleSetWidthPercent(val.Value.Value);
                }
            }
            else
            {
                if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
                {
                    node!.StyleSetWidth(val.Value.Value);
                }
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexWidthBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexWidthBehavior>(out var val))
        {
            if (val.Value.Percent)
            {
                if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
                {
                    node!.StyleSetWidthPercent(val.Value.Value);
                }
            }
            else
            {
                if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
                {
                    node!.StyleSetWidth(val.Value.Value);
                }
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexWidthBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
        {
            node!.StyleSetWidth(float.NaN);
        }
    }

    // Height
    public void OnEvent(BehaviorAddedEvent<FlexHeightBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexHeightBehavior>(out var val))
        {
            if (val.Value.Percent)
            {
                if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
                {
                    node!.StyleSetHeightPercent(val.Value.Value);
                }
            }
            else
            {
                if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
                {
                    node!.StyleSetHeight(val.Value.Value);
                }
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexHeightBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (e.Entity.TryGetBehavior<FlexHeightBehavior>(out var val))
        {
            if (val.Value.Percent)
            {
                if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
                {
                    node!.StyleSetHeightPercent(val.Value.Value);
                }
            }
            else
            {
                if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
                {
                    node!.StyleSetHeight(val.Value.Value);
                }
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexHeightBehavior> e)
    {
        _dirty.Set(e.Entity.Index.ToInt(), true);
        if (!_nodes.HasValue(e.Entity.Index.ToInt()))
        {
            _nodes[e.Entity.Index.ToInt()] = FlexLayoutSharp.Flex.CreateDefaultNode();
        }
        if (_nodes.TryGetValue(e.Entity.Index.ToInt(), out var node))
        {
            node!.StyleSetHeight(float.NaN);
        }
    }
}
