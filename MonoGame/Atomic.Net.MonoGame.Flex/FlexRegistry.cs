using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;
using Atomic.Net.MonoGame.Core;
using FlexLayoutSharp;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    ISingleton<FlexRegistry>,
    IEventHandler<InitializeEvent>,
    // Flex core behaviors
    IEventHandler<BehaviorAddedEvent<FlexBehavior>>,
    IEventHandler<BehaviorRemovedEvent<FlexBehavior>>,
    // Enable/Disable
    IEventHandler<EntityEnabledEvent>,
    IEventHandler<EntityDisabledEvent>
{
    public static FlexRegistry Instance { get; } = new();

    private readonly Node?[] _nodes = new Node?[Constants.MaxEntities];
    private readonly bool[] _dirty = new bool[Constants.MaxEntities];

    private FlexRegistry()
    {
        EventBus<InitializeEvent>.Register(this);
    }

    public void OnEvent(InitializeEvent _)
    {
        EventBus<BehaviorAddedEvent<FlexBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<FlexBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexZOverride>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexZOverride>>.Register(this);
        EventBus<BehaviorRemovedEvent<FlexZOverride>>.Register(this);

        // Flex layout properties
        EventBus<BehaviorAddedEvent<FlexDirectionBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexDirectionBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<FlexDirectionBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexWrapBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexWrapBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<FlexWrapBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<JustifyContentBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<JustifyContentBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<JustifyContentBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<AlignItemsBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<AlignItemsBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<AlignItemsBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<AlignContentBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<AlignContentBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<AlignContentBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<AlignSelfBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<AlignSelfBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<AlignSelfBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexGrowBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexGrowBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<FlexGrowBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexShrinkBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexShrinkBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<FlexShrinkBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<PositionTypeBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<PositionTypeBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<PositionTypeBehavior>>.Register(this);

        // Position edges
        EventBus<BehaviorAddedEvent<PositionLeftBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<PositionLeftBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<PositionLeftBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<PositionRightBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<PositionRightBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<PositionRightBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<PositionTopBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<PositionTopBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<PositionTopBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<PositionBottomBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<PositionBottomBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<PositionBottomBehavior>>.Register(this);

        // Margins
        EventBus<BehaviorAddedEvent<MarginLeftBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<MarginLeftBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<MarginLeftBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<MarginRightBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<MarginRightBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<MarginRightBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<MarginTopBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<MarginTopBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<MarginTopBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<MarginBottomBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<MarginBottomBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<MarginBottomBehavior>>.Register(this);

        // Padding
        EventBus<BehaviorAddedEvent<PaddingLeftBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<PaddingLeftBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<PaddingLeftBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<PaddingRightBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<PaddingRightBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<PaddingRightBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<PaddingTopBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<PaddingTopBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<PaddingTopBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<PaddingBottomBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<PaddingBottomBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<PaddingBottomBehavior>>.Register(this);

        // Borders
        EventBus<BehaviorAddedEvent<BorderLeftBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<BorderLeftBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<BorderLeftBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<BorderRightBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<BorderRightBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<BorderRightBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<BorderTopBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<BorderTopBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<BorderTopBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<BorderBottomBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<BorderBottomBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<BorderBottomBehavior>>.Register(this);

        // Width / Height
        EventBus<BehaviorAddedEvent<WidthBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<WidthBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<WidthBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<HeightBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<HeightBehavior>>.Register(this);
        EventBus<BehaviorRemovedEvent<HeightBehavior>>.Register(this);

        // Enable/Disable
        EventBus<EntityEnabledEvent>.Register(this);
        EventBus<EntityDisabledEvent>.Register(this);
    }

    public void Recalculate()
    {
        for (ushort i = 0; i < Constants.MaxEntities; i++)
        {
            RecalculateNode(i);
        }
    }

    private void RecalculateNode(ushort index)
    {
        if (!_dirty[index])
        {
            return;
        }
        _dirty[index] = false;
        

        // Check if we have a flex parent, if so run on that instead
        if (BehaviorRegistry<Parent>.Instance.TryGetBehavior(index, out var parent))
        {
            if (BehaviorRegistry<FlexBehavior>.Instance.HasBehavior(EntityRegistry.Instance[parent.Value.ParentIndex]))
            {
                RecalculateNode(parent.Value.ParentIndex);
                return;
            }
        }
        var root = EntityRegistry.Instance[index];

        // Otherwise we are a root node, so confirm we are a flex node
        if (!root.HasBehavior<FlexBehavior>())
        {
            return;
        }

        _nodes[index]!.CalculateLayout(float.NaN, float.NaN, Direction.Inherit);

        UpdateFlexBehavior(root, 0, 0);
    }

    private void UpdateFlexBehavior(Entity e, float parentLeft, float parentTop, int zIndex = 0)
    {
        if (!e.HasBehavior<FlexBehavior>())
        {
            return;
        }

        var node = _nodes[e.Index]!;

        var paddingLeft = parentLeft + node.LayoutGetLeft();
        var paddingTop = parentTop + node.LayoutGetTop();
        var paddingWidth = node.LayoutGetWidth();
        var paddingHeight = node.LayoutGetHeight();

        var marginLeft = paddingLeft - node.LayoutGetMargin(Edge.Left);
        var contentLeft = paddingLeft + node.LayoutGetPadding(Edge.Left);

        var marginWidth = paddingWidth + node.LayoutGetMargin(Edge.Left) + node.LayoutGetMargin(Edge.Right);
        var contentWidth = paddingWidth - node.LayoutGetPadding(Edge.Left) - node.LayoutGetPadding(Edge.Right);

        var marginTop = paddingTop - node.LayoutGetMargin(Edge.Top);
        var contentTop = paddingTop + node.LayoutGetPadding(Edge.Top);

        var marginHeight = paddingHeight + node.LayoutGetMargin(Edge.Top) + node.LayoutGetMargin(Edge.Bottom);
        var contentHeight = paddingHeight - node.LayoutGetPadding(Edge.Top) - node.LayoutGetPadding(Edge.Bottom);

        var borderTop = node.LayoutGetBorder(Edge.Top);
        var borderRight = node.LayoutGetBorder(Edge.Right);
        var borderBottom = node.LayoutGetBorder(Edge.Bottom);
        var borderLeft = node.LayoutGetBorder(Edge.Left);

        var truezIndex = zIndex;
        if (e.TryGetBehavior<FlexZOverride>(out var zOverride))
        {
            truezIndex = zOverride.Value.ZIndex;
        }

        e.SetBehavior((ref FlexBehavior v) =>
            v = new FlexBehavior(
                MarginRect: new(marginLeft, marginTop, marginWidth, marginHeight),
                PaddingRect: new(paddingLeft, paddingTop, paddingWidth, paddingHeight),
                ContentRect: new(contentLeft, contentTop, contentWidth, contentHeight),
                BorderLeft: borderLeft,
                BorderTop: borderTop,
                BorderRight: borderRight,
                BorderBottom: borderBottom,
                ZIndex: truezIndex
            )
        ); 

        foreach(var child in e.GetChildren())
        {
            UpdateFlexBehavior(e, paddingLeft, paddingTop, zIndex+1);
        }
    }

    public void OnEvent(BehaviorAddedEvent<FlexBehavior> e)
    {
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _dirty[e.Entity.Index] = true;
    }

    public void OnEvent(BehaviorRemovedEvent<FlexBehavior> e)
    {
        _nodes[e.Entity.Index] = null;
        // Special case, because this node isnt even a flex anymore
        if (e.Entity.TryGetParent(out var parent))
        {
            _dirty[parent.Value.Index] = true;
        }
    }

    public void OnEvent(EntityEnabledEvent e)
    {
        var node = _nodes[e.Entity.Index];
        if(node == null)
        {
            return;
        }

        _dirty[e.Entity.Index] = true;
        node.StyleSetDisplay(Display.Flex);
    }

    public void OnEvent(EntityDisabledEvent e)
    {
        var node = _nodes[e.Entity.Index];
        if(node == null)
        {
            return;
        }

        _dirty[e.Entity.Index] = true;
        node.StyleSetDisplay(Display.None);
    }
}
