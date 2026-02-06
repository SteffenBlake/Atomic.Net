using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Hierarchy;
using Atomic.Net.MonoGame.Transform;
using FlexLayoutSharp;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Flex;

public partial class FlexRegistry :
    ISingleton<FlexRegistry>,
    IEventHandler<InitializeEvent>,
    IEventHandler<ShutdownEvent>,
    // Flex core behaviors
    IEventHandler<BehaviorAddedEvent<FlexBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexBehavior>>,
    // Parent hierarchy for flex tree management
    IEventHandler<BehaviorAddedEvent<ParentBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<ParentBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<ParentBehavior>>,
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
        EventBus<ShutdownEvent>.Register(Instance);
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

    // BUG-03 FIX: Track visited nodes during RecalculateNode to detect cycles
    private readonly PartitionedSparseArray<bool> _visitedThisFrame = new(
        Constants.MaxGlobalEntities,
        Constants.MaxSceneEntities
    );

    private readonly PartitionedSparseArray<bool> _flexTreeDirty = new(
        Constants.MaxGlobalEntities,
        Constants.MaxSceneEntities
    );

    // Reverse lookup: Node -> Entity Index
    // Enables O(1) parent entity lookup when removing child from flex tree
    private readonly Dictionary<Node, PartitionIndex> _nodeToEntity = [];

    /// <summary>
    /// Ensures a flex node exists for the entity and marks it dirty.
    /// Returns the node for further configuration.
    /// </summary>
    protected Node EnsureDirtyNode(PartitionIndex index)
    {
        _dirty.Set(index, true);

        // If this entity has a flex parent, mark the parent dirty too
        // This ensures that when child properties change, the parent's layout is recalculated
        var entity = EntityRegistry.Instance[index];
        if (BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(index, out var parentBehavior))
        {
            if (parentBehavior.Value.TryFindParent(entity.IsGlobal(), out var parent))
            {
                if (parent.Value.Active && parent.Value.HasBehavior<FlexBehavior>())
                {
                    _dirty.Set(parent.Value.Index, true);
                }
            }
        }

        if (!_nodes.TryGetValue(index, out var node))
        {
            node = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[index] = node;
            _nodeToEntity[node] = index;
        }
        return node;
    }

    public void Recalculate()
    {
        // BUG-03 FIX: Clear visited tracking at start of each Recalculate
        _visitedThisFrame.Global.Clear();
        _visitedThisFrame.Scene.Clear();

        // First, update flex tree for any entities that had parent changes
        foreach (var (index, _) in _flexTreeDirty.Global)
        {
            UpdateFlexTreeForEntity((ushort)index);
        }
        foreach (var (index, _) in _flexTreeDirty.Scene)
        {
            UpdateFlexTreeForEntity((uint)index);
        }
        _flexTreeDirty.Global.Clear();
        _flexTreeDirty.Scene.Clear();

        // Then recalculate layout for dirty nodes
        foreach (var (index, _) in _dirty.Global)
        {
            RecalculateNode((ushort)index);
        }
        foreach (var (index, _) in _dirty.Scene)
        {
            RecalculateNode((uint)index);
        }
    }

    /// <summary>
    /// Updates the flex tree structure for an entity by removing it from its old parent
    /// and adding it to its new parent if both the entity and parent have FlexBehavior.
    /// Called during Recalc() when parent relationships change.
    /// </summary>
    /// <remarks>
    /// This method handles complex state management:
    /// - Removes entity from old parent node (if exists) using Node.GetParent()
    /// - Adds entity to new parent node only if parent has FlexBehavior
    /// - Prevents duplicate children by checking IndexOfChild before AddChild
    /// - Marks parent entities dirty for layout recalculation (O(1) via _nodeToEntity lookup)
    /// </remarks>
    private void UpdateFlexTreeForEntity(PartitionIndex entityIndex)
    {
        var entity = EntityRegistry.Instance[entityIndex];

        if (!entity.Active || !entity.HasBehavior<FlexBehavior>())
        {
            return;
        }

        // TryGetValue on SparseReferenceArray guarantees non-null on success for reference types
        if (!_nodes.TryGetValue(entityIndex, out var myNode))
        {
            return;
        }

        // Remove from any current parent first (if it exists)
        RemoveFromParentFlexTree(entity);

        // Then add to new parent if it has FlexBehavior
        if (!entity.TryGetParent(out var parent) || !parent.Value.HasBehavior<FlexBehavior>())
        {
            return;
        }

        if (!_nodes.TryGetValue(parent.Value.Index, out var parentNode))
        {
            return;
        }

        // Defensive check: IndexOfChild ensures we don't add duplicate children
        // This can occur during Recalc when multiple parent change events fire
        // for the same entity within a single frame
        if (parentNode.IndexOfChild(myNode) == -1)
        {
            parentNode.AddChild(myNode);
            _dirty.Set(parent.Value.Index, true);
        }
    }

    private void RecalculateNode(PartitionIndex index)
    {
        // BUG-03 FIX: Check visited BEFORE dirty check
        // This MUST be first or cycles won't be detected (dirty flags removed before recursion)
        if (_visitedThisFrame.HasValue(index))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Circular parent reference detected in flex hierarchy at entity {index}"
            ));
            return;
        }
        _visitedThisFrame.Set(index, true);

        // Now check dirty (after visited check)
        if (!_dirty.HasValue(index))
        {
            return;
        }
        _dirty.Remove(index);

        var entity = EntityRegistry.Instance[index];

        // Skip inactive entities (dirty flags persist across scene resets)
        if (!entity.Active)
        {
            return;
        }

        // Check if we have a flex parent, if so run on that instead
        if (BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(index, out var parentBehavior))
        {
            if (parentBehavior.Value.TryFindParent(entity.IsGlobal(), out var parent))
            {
                if (parent.Value.Active && parent.Value.HasBehavior<FlexBehavior>())
                {
                    RecalculateNode(parent.Value.Index);
                    return;
                }
            }
        }

        // Otherwise we are a root node, so confirm we are a flex node
        if (!entity.HasBehavior<FlexBehavior>())
        {
            return;
        }

        if (_nodes.TryGetValue(index, out var node))
        {
            node.CalculateLayout(float.NaN, float.NaN, Direction.Inherit);
        }

        UpdateFlexBehavior(entity);
    }

    private void UpdateFlexBehavior(Entity e, int zIndex = 0)
    {
        // Skip inactive entities
        if (!e.Active)
        {
            return;
        }

        if (!e.HasBehavior<FlexBehavior>())
        {
            return;
        }

        if (!_nodes.TryGetValue(e.Index, out var node))
        {
            return;
        }

        // Local position relative to parent (this becomes TransformBehavior.Position)
        var localX = node.LayoutGetLeft();
        var localY = node.LayoutGetTop();

        // senior-dev: FINDING: Yoga's LayoutGetWidth/Height returns BORDER BOX dimensions
        // (content + padding + border), NOT padding box or content box. This was causing
        // incorrect calculation of PaddingRect and ContentRect. The correct calculation is:
        // - borderWidth/Height = LayoutGetWidth/Height() (from Yoga)
        // - paddingWidth/Height = borderWidth/Height - borderLeft - borderRight/Top/Bottom
        // - contentWidth/Height = paddingWidth/Height - paddingLeft/Right/Top/Bottom
        // This matches CSS box-sizing: border-box behavior.

        // Dimensions from Yoga (content + padding + border, NOT including margin)
        var borderWidth = node.LayoutGetWidth();
        var borderHeight = node.LayoutGetHeight();

        // Get border values
        var borderTop = node.LayoutGetBorder(Edge.Top);
        var borderRight = node.LayoutGetBorder(Edge.Right);
        var borderBottom = node.LayoutGetBorder(Edge.Bottom);
        var borderLeft = node.LayoutGetBorder(Edge.Left);

        // Get padding values
        var paddingTop = node.LayoutGetPadding(Edge.Top);
        var paddingRight = node.LayoutGetPadding(Edge.Right);
        var paddingBottom = node.LayoutGetPadding(Edge.Bottom);
        var paddingLeft = node.LayoutGetPadding(Edge.Left);

        // Get margin values
        var marginTop = node.LayoutGetMargin(Edge.Top);
        var marginRight = node.LayoutGetMargin(Edge.Right);
        var marginBottom = node.LayoutGetMargin(Edge.Bottom);
        var marginLeft = node.LayoutGetMargin(Edge.Left);

        // Calculate padding box dimensions (border box - borders)
        var paddingWidth = borderWidth - borderLeft - borderRight;
        var paddingHeight = borderHeight - borderTop - borderBottom;

        // Calculate content box dimensions (padding box - padding)
        var contentWidth = paddingWidth - paddingLeft - paddingRight;
        var contentHeight = paddingHeight - paddingTop - paddingBottom;

        // Calculate rectangle positions and dimensions in local coordinates
        // MarginRect starts at negative margin offset and includes everything
        var marginRectX = -marginLeft;
        var marginRectY = -marginTop;
        var marginRectWidth = borderWidth + marginLeft + marginRight;
        var marginRectHeight = borderHeight + marginTop + marginBottom;

        // PaddingRect starts at border offset (inside the border)
        var paddingRectX = borderLeft;
        var paddingRectY = borderTop;

        // ContentRect starts at border + padding offset
        var contentRectX = borderLeft + paddingLeft;
        var contentRectY = borderTop + paddingTop;

        var truezIndex = zIndex;
        if (e.TryGetBehavior<FlexZOverride>(out var zOverride))
        {
            truezIndex = zOverride.Value.ZIndex;
        }

        // Store LOCAL flex rectangles (relative to entity's own position)
        var helper = new FlexBehavior(
            MarginRect: new(marginRectX, marginRectY, marginRectWidth, marginRectHeight),
            PaddingRect: new(paddingRectX, paddingRectY, paddingWidth, paddingHeight),
            ContentRect: new(contentRectX, contentRectY, contentWidth, contentHeight),
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
        // BUT: Only set position for non-root flex entities (entities with flex parents)
        // Root flex entities keep their original Transform.Position (e.g., from JSON)
        if (BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(e.Index, out var parentBehavior))
        {
            if (parentBehavior.Value.TryFindParent(e.IsGlobal(), out var parent))
            {
                if (parent.Value.Active && parent.Value.HasBehavior<FlexBehavior>())
                {
                    // Child of flex parent: position is determined by flex layout
                    // Yoga returns position relative to parent's padding box (after border, includes padding area)
                    // Transform.Position needs to be relative to parent's origin (border box edge)
                    // So we add parent's border to convert coordinate systems
                    var adjustedX = localX;
                    var adjustedY = localY;

                    if (_nodes.TryGetValue(parent.Value.Index, out var parentNode))
                    {
                        adjustedX += parentNode.LayoutGetBorder(Edge.Left);
                        adjustedY += parentNode.LayoutGetBorder(Edge.Top);
                    }

                    var localPosition = new Vector3(adjustedX, adjustedY, 0);
                    e.SetBehavior<TransformBehavior, Vector3>(
                        in localPosition,
                        static (ref readonly pos, ref transform) => transform = transform with { Position = pos }
                    );
                }
            }
        }
        // else: Root flex entity keeps its original Transform.Position

        foreach (var child in e.GetChildren())
        {
            UpdateFlexBehavior(child, zIndex + 1);
        }
    }

    public void OnEvent(BehaviorAddedEvent<FlexBehavior> e)
    {
        if (!_nodes.TryGetValue(e.Entity.Index, out var existingNode))
        {
            var newNode = FlexLayoutSharp.Flex.CreateDefaultNode();
            _nodes[e.Entity.Index] = newNode;
            _nodeToEntity[newNode] = e.Entity.Index;
        }
        _dirty.Set(e.Entity.Index, true);
        _flexTreeDirty.Set(e.Entity.Index, true); // Mark for flex tree update

        // Ensure TransformBehavior exists for flex entities
        if (!e.Entity.HasBehavior<TransformBehavior>())
        {
            e.Entity.SetBehavior<TransformBehavior>(static (ref _) => { });
        }
    }

    public void OnEvent(BehaviorAddedEvent<ParentBehavior> e)
    {
        // If this entity has FlexBehavior, mark for flex tree update
        if (e.Entity.HasBehavior<FlexBehavior>())
        {
            _flexTreeDirty.Set(e.Entity.Index, true);
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<ParentBehavior> e)
    {
        // If this entity has FlexBehavior, mark for flex tree update
        if (e.Entity.HasBehavior<FlexBehavior>())
        {
            _flexTreeDirty.Set(e.Entity.Index, true);
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<ParentBehavior> e)
    {
        // If this entity has FlexBehavior, mark for flex tree update
        if (e.Entity.HasBehavior<FlexBehavior>())
        {
            _flexTreeDirty.Set(e.Entity.Index, true);
        }
    }

    /// <summary>
    /// Removes an entity's flex node from its parent node in the flex tree.
    /// Uses O(1) reverse lookup via _nodeToEntity dictionary to find and mark parent entity dirty.
    /// </summary>
    /// <remarks>
    /// Performance characteristics:
    /// - Node parent lookup: O(1) via Node.GetParent()
    /// - Parent entity lookup: O(1) via _nodeToEntity dictionary
    /// - Child removal: O(n) where n = number of children in parent (FlexLayoutSharp limitation)
    /// - Total: O(n) where n = children count, NOT entity count
    /// 
    /// Called when:
    /// - Entity parent changes (need to remove from old parent before adding to new)
    /// - FlexBehavior is removed from entity
    /// </remarks>
    private void RemoveFromParentFlexTree(Entity entity)
    {
        if (!_nodes.TryGetValue(entity.Index, out var myNode))
        {
            return;
        }

        // Use Node.GetParent() to get the node's current parent in the flex tree
        // (entity's parent might have already changed, but node's parent hasn't)
        var parentNode = myNode.GetParent();
        if (parentNode is not null && parentNode.IndexOfChild(myNode) != -1)
        {
            parentNode.RemoveChild(myNode);

            // Use O(1) reverse lookup to find parent entity and mark it dirty
            if (_nodeToEntity.TryGetValue(parentNode, out var parentIndex))
            {
                _dirty.Set(parentIndex, true);
            }
            else
            {
                // Defensive: This should never happen in normal operation since we maintain
                // _nodeToEntity in sync with _nodes. If we reach here, it indicates a bug
                // in the registry's internal state management.
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    "FlexRegistry internal error: Parent node exists in flex tree but not in _nodeToEntity lookup. " +
                    $"Entity: {entity.Index}, Parent node will not be marked dirty for recalculation."
                ));
            }
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<FlexBehavior> e)
    {
        // BUG-02 FIX: Mark parent dirty when child's FlexBehavior is removed
        // Parent needs to recalculate layout without this child
        if (e.Entity.TryGetParent(out var parent))
        {
            if (parent.Value.HasBehavior<FlexBehavior>())
            {
                _dirty.Set(parent.Value.Index, true);
            }
        }

        // Remove from parent's children first
        RemoveFromParentFlexTree(e.Entity);

        // Then remove the node itself and its reverse lookup
        if (_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            _nodeToEntity.Remove(node);
        }
        _nodes.Remove(e.Entity.Index);
    }

    public void OnEvent(EntityEnabledEvent e)
    {
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            return;
        }

        _dirty.Set(e.Entity.Index, true);
        node.StyleSetDisplay(Display.Flex);
    }

    public void OnEvent(EntityDisabledEvent e)
    {
        if (!_nodes.TryGetValue(e.Entity.Index, out var node))
        {
            return;
        }

        _dirty.Set(e.Entity.Index, true);
        node.StyleSetDisplay(Display.None);
    }

    public void OnEvent(ShutdownEvent _)
    {
        // Clear all internal state to prevent pollution between tests
        _dirty.Global.Clear();
        _dirty.Scene.Clear();
        _visitedThisFrame.Global.Clear();
        _visitedThisFrame.Scene.Clear();
        _flexTreeDirty.Global.Clear();
        _flexTreeDirty.Scene.Clear();
        _nodeToEntity.Clear();

        // Clear nodes (entities are being deactivated, which will trigger PreBehaviorRemovedEvent)
        // But we also clear here to ensure no stale references
        _nodes.Global.Clear();
        _nodes.Scene.Clear();
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

        // Parent hierarchy
        EventBus<BehaviorAddedEvent<ParentBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<ParentBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<ParentBehavior>>.Register(this);

        // Enable/Disable
        EventBus<EntityEnabledEvent>.Register(this);
        EventBus<EntityDisabledEvent>.Register(this);
    }
}
