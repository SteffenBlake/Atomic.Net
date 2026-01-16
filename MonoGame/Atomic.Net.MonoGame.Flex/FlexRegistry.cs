using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;
using Atomic.Net.MonoGame.Core;
using FlexLayoutSharp;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry : 
    ISingleton<FlexRegistry>,
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

    public static void Register()
    {
        EventBus<BehaviorAddedEvent<FlexBehavior>>.Register<FlexRegistry>();

        // Flex core behaviors
        EventBus<BehaviorAddedEvent<FlexBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<FlexBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<FlexZOverride>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<FlexZOverride>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<FlexZOverride>>.Register<FlexRegistry>();

        // Flex layout properties
        EventBus<BehaviorAddedEvent<FlexDirectionBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<FlexDirectionBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<FlexDirectionBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<FlexWrapBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<FlexWrapBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<FlexWrapBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<JustifyContentBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<JustifyContentBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<JustifyContentBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<AlignItemsBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<AlignItemsBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<AlignItemsBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<AlignContentBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<AlignContentBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<AlignContentBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<AlignSelfBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<AlignSelfBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<AlignSelfBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<FlexGrowBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<FlexGrowBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<FlexGrowBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<FlexShrinkBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<FlexShrinkBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<FlexShrinkBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<PositionTypeBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<PositionTypeBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<PositionTypeBehavior>>.Register<FlexRegistry>();

        // Position edges
        EventBus<BehaviorAddedEvent<PositionLeftBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<PositionLeftBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<PositionLeftBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<PositionRightBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<PositionRightBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<PositionRightBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<PositionTopBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<PositionTopBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<PositionTopBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<PositionBottomBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<PositionBottomBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<PositionBottomBehavior>>.Register<FlexRegistry>();

        // Margins
        EventBus<BehaviorAddedEvent<MarginLeftBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<MarginLeftBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<MarginLeftBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<MarginRightBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<MarginRightBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<MarginRightBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<MarginTopBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<MarginTopBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<MarginTopBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<MarginBottomBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<MarginBottomBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<MarginBottomBehavior>>.Register<FlexRegistry>();

        // Padding
        EventBus<BehaviorAddedEvent<PaddingLeftBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<PaddingLeftBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<PaddingLeftBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<PaddingRightBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<PaddingRightBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<PaddingRightBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<PaddingTopBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<PaddingTopBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<PaddingTopBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<PaddingBottomBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<PaddingBottomBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<PaddingBottomBehavior>>.Register<FlexRegistry>();

        // Borders
        EventBus<BehaviorAddedEvent<BorderLeftBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<BorderLeftBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<BorderLeftBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<BorderRightBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<BorderRightBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<BorderRightBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<BorderTopBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<BorderTopBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<BorderTopBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<BorderBottomBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<BorderBottomBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<BorderBottomBehavior>>.Register<FlexRegistry>();

        // Width / Height
        EventBus<BehaviorAddedEvent<WidthBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<WidthBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<WidthBehavior>>.Register<FlexRegistry>();

        EventBus<BehaviorAddedEvent<HeightBehavior>>.Register<FlexRegistry>();
        EventBus<PostBehaviorUpdatedEvent<HeightBehavior>>.Register<FlexRegistry>();
        EventBus<BehaviorRemovedEvent<HeightBehavior>>.Register<FlexRegistry>();

        // Enable/Disable
        EventBus<EntityEnabledEvent>.Register<FlexRegistry>();
        EventBus<EntityDisabledEvent>.Register<FlexRegistry>();
    }
}
