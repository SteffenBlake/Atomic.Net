using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Hierarchy;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Transform;
using FlexLayoutSharp;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry :
    ISingleton<FlexRegistry>,
    IEventHandler<InitializeEvent>,
    // Flex core behaviors
    IEventHandler<BehaviorAddedEvent<FlexBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexBehavior>>,
    // Enable/Disable
    IEventHandler<EntityEnabledEvent>,
    IEventHandler<EntityDisabledEvent>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
        EventBus<InitializeEvent>.Register(Instance);
    }

    public static FlexRegistry Instance { get; private set; } = null!;

    private readonly PartitionedSparseRefArray<Node> _nodes = new(
        Constants.MaxGlobalEntities,
        Constants.MaxSceneEntities
    );
    private readonly PartitionedSparseArray<bool> _dirty = new(
        Constants.MaxGlobalEntities,
        Constants.MaxSceneEntities
    );

    /// <summary>
    /// Ensures a flex node exists for the entity and marks it dirty.
    /// Returns the node for further configuration.
    /// </summary>
    protected Node EnsureDirtyNode(PartitionIndex index)
    {
        _dirty.Set(index, true);
        if (!_nodes.TryGetValue(index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[index] = node;
        }
        return node;
    }

    public void Recalculate()
    {
        // Rebuild flex tree hierarchy from entity parent-child relationships
        RebuildFlexTree();

        // Recalculate global partition
        foreach (var (index, _) in _dirty.Global)
        {
            RecalculateNode((ushort)index);
        }

        // Recalculate scene partition
        foreach (var (index, _) in _dirty.Scene)
        {
            RecalculateNode((uint)index);
        }
    }

    private void RebuildFlexTree()
    {
        // Clear all parent-child relationships in flex nodes
        foreach (var (index, node) in _nodes.Global)
        {
            if (node != null)
            {
                while (true)
                {
                    try { node.RemoveChild(node.GetChild(0)); }
                    catch { break; }
                }
            }
        }
        foreach (var (index, node) in _nodes.Scene)
        {
            if (node != null)
            {
                while (true)
                {
                    try { node.RemoveChild(node.GetChild(0)); }
                    catch { break; }
                }
            }
        }

        // Rebuild relationships from entity hierarchy
        foreach (var (index, node) in _nodes.Global)
        {
            if (node == null) continue;
            var entity = EntityRegistry.Instance[(ushort)index];
            if (!entity.Active || !entity.HasBehavior<FlexBehavior>()) continue;

            if (entity.TryGetParent(out var parent) && parent.Value.HasBehavior<FlexBehavior>())
            {
                if (_nodes.TryGetValue(parent.Value.Index, out var parentNode) && parentNode != null)
                {
                    var childIndex = 0;
                    while (true)
                    {
                        try { _ = parentNode.GetChild(childIndex); childIndex++; }
                        catch { break; }
                    }
                    parentNode.InsertChild(node, childIndex);
                }
            }
        }
        foreach (var (index, node) in _nodes.Scene)
        {
            if (node == null) continue;
            var entity = EntityRegistry.Instance[(uint)index];
            if (!entity.Active || !entity.HasBehavior<FlexBehavior>()) continue;

            if (entity.TryGetParent(out var parent) && parent.Value.HasBehavior<FlexBehavior>())
            {
                if (_nodes.TryGetValue(parent.Value.Index, out var parentNode) && parentNode != null)
                {
                    var childIndex = 0;
                    while (true)
                    {
                        try { _ = parentNode.GetChild(childIndex); childIndex++; }
                        catch { break; }
                    }
                    parentNode.InsertChild(node, childIndex);
                }
            }
        }
    }

    private void RecalculateNode(PartitionIndex index)
    {
        if (!_dirty.HasValue(index))
        {
            return;
        }
        _dirty.Remove(index);


        // Check if we have a flex parent, if so run on that instead
        if (BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(index, out var parentBehavior))
        {
            var entity = EntityRegistry.Instance[index];
            if (parentBehavior.Value.TryFindParent(entity.IsGlobal(), out var parent))
            {
                if (parent.Value.HasBehavior<FlexBehavior>())
                {
                    RecalculateNode(parent.Value.Index);
                    return;
                }
            }
        }
        var root = EntityRegistry.Instance[index];

        // Otherwise we are a root node, so confirm we are a flex node
        if (!root.HasBehavior<FlexBehavior>())
        {
            return;
        }

        if (_nodes.TryGetValue(index, out var node))
        {
            node!.CalculateLayout(float.NaN, float.NaN, Direction.Inherit);
        }

        UpdateFlexBehavior(root, 0, 0);
    }

    private void UpdateFlexBehavior(Entity e, float parentLeft, float parentTop, int zIndex = 0)
    {
        if (!e.HasBehavior<FlexBehavior>())
        {
            return;
        }

        if (!_nodes.TryGetValue(e.Index, out var node))
        {
            return;
        }

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

        var helper = new FlexBehavior(
            MarginRect: new(marginLeft, marginTop, marginWidth, marginHeight),
            PaddingRect: new(paddingLeft, paddingTop, paddingWidth, paddingHeight),
            ContentRect: new(contentLeft, contentTop, contentWidth, contentHeight),
            BorderLeft: borderLeft,
            BorderTop: borderTop,
            BorderRight: borderRight,
            BorderBottom: borderBottom,
            ZIndex: truezIndex
        );

        e.SetBehavior<FlexBehavior, FlexBehavior>(
            in helper,
            static (ref readonly h, ref v) => v = h
        );

        // Set TransformBehavior based on flex layout (local position relative to parent)
        var localPosition = new Vector3(node.LayoutGetLeft(), node.LayoutGetTop(), 0);
        e.SetBehavior<TransformBehavior, Vector3>(
            in localPosition,
            static (ref readonly pos, ref transform) => transform = transform with { Position = pos }
        );

        foreach (var child in e.GetChildren())
        {
            UpdateFlexBehavior(child, paddingLeft, paddingTop, zIndex + 1);
        }
    }

    public void OnEvent(BehaviorAddedEvent<FlexBehavior> e)
    {
        _nodes[e.Entity.Index] ??= FlexLayoutSharp.Flex.CreateDefaultNode();
        _dirty.Set(e.Entity.Index, true);

        // Ensure TransformBehavior exists for flex entities
        if (!e.Entity.HasBehavior<TransformBehavior>())
        {
            e.Entity.SetBehavior<TransformBehavior>(static (ref _) => { });
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexBehavior> e)
    {
        _nodes.Remove(e.Entity.Index);
        // Special case, because this node isnt even a flex anymore
        if (e.Entity.TryGetParent(out var parent))
        {
            _dirty.Set(parent.Value.Index, true);
        }
    }

    public void OnEvent(EntityEnabledEvent e)
    {
        if (!_nodes.TryGetValue(e.Entity.Index, out var node) || node == null)
        {
            return;
        }

        _dirty.Set(e.Entity.Index, true);
        node.StyleSetDisplay(Display.Flex);
    }

    public void OnEvent(EntityDisabledEvent e)
    {
        if (!_nodes.TryGetValue(e.Entity.Index, out var node) || node == null)
        {
            return;
        }

        _dirty.Set(e.Entity.Index, true);
        node.StyleSetDisplay(Display.None);
    }

    public void OnEvent(InitializeEvent _)
    {
        EventBus<BehaviorAddedEvent<FlexBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexZOverride>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexZOverride>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexZOverride>>.Register(this);

        // Flex layout properties
        EventBus<BehaviorAddedEvent<FlexDirectionBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexDirectionBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexDirectionBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexWrapBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexWrapBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexWrapBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexJustifyContentBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexJustifyContentBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexJustifyContentBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexAlignItemsBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexAlignItemsBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexAlignItemsBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<AlignContentBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<AlignContentBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<AlignContentBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexAlignSelfBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexAlignSelfBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexAlignSelfBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexGrowBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexGrowBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexGrowBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexShrinkBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexShrinkBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexShrinkBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexPositionTypeBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexPositionTypeBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexPositionTypeBehavior>>.Register(this);

        // Position edges
        EventBus<BehaviorAddedEvent<FlexPositionLeftBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexPositionLeftBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexPositionLeftBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexPositionRightBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexPositionRightBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexPositionRightBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexPositionTopBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexPositionTopBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexPositionTopBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexPositionBottomBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexPositionBottomBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexPositionBottomBehavior>>.Register(this);

        // Margins
        EventBus<BehaviorAddedEvent<FlexMarginLeftBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexMarginLeftBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexMarginLeftBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexMarginRightBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexMarginRightBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexMarginRightBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexMarginTopBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexMarginTopBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexMarginTopBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexMarginBottomBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexMarginBottomBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexMarginBottomBehavior>>.Register(this);

        // Padding
        EventBus<BehaviorAddedEvent<FlexPaddingLeftBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexPaddingLeftBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexPaddingLeftBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexPaddingRightBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexPaddingRightBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexPaddingRightBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexPaddingTopBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexPaddingTopBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexPaddingTopBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexPaddingBottomBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexPaddingBottomBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexPaddingBottomBehavior>>.Register(this);

        // Borders
        EventBus<BehaviorAddedEvent<FlexBorderLeftBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexBorderLeftBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexBorderLeftBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexBorderRightBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexBorderRightBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexBorderRightBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexBorderTopBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexBorderTopBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexBorderTopBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexBorderBottomBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexBorderBottomBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexBorderBottomBehavior>>.Register(this);

        // Width / Height
        EventBus<BehaviorAddedEvent<FlexWidthBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexWidthBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexWidthBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<FlexHeightBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexHeightBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexHeightBehavior>>.Register(this);

        // Enable/Disable
        EventBus<EntityEnabledEvent>.Register(this);
        EventBus<EntityDisabledEvent>.Register(this);
    }
}
