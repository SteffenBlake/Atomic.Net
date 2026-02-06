using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Ids;

namespace Atomic.Net.MonoGame.Hierarchy;

/// <summary>
/// Tracks parent-to-child relationships between entities using a fixed-size jagged boolean array.
/// This registry does not store any other state and serves as a fast lookup for hierarchical queries.
/// </summary>
public class HierarchyRegistry :
    ISingleton<HierarchyRegistry>,
    IEventHandler<BehaviorAddedEvent<ParentBehavior>>,
    IEventHandler<PreBehaviorUpdatedEvent<ParentBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<ParentBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<ParentBehavior>>,
    IEventHandler<PreEntityDeactivatedEvent>,
    IEventHandler<InitializeEvent>,
    IEventHandler<ShutdownEvent>
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

    public static HierarchyRegistry Instance { get; private set; } = null!;

    // Global parents with global children only
    private readonly SparseReferenceArray<SparseArray<bool>> _globalParentToChildren
        = new(Constants.MaxGlobalEntities);

    // Scene parents with scene children only
    private readonly SparseReferenceArray<SparseArray<bool>> _sceneParentToChildren
        = new(Constants.MaxSceneEntities);

    // Track dirty children who need parent resolution during Recalc
    private readonly PartitionedSparseArray<bool> _dirtyChildren = new(
        Constants.MaxGlobalEntities,
        Constants.MaxSceneEntities
    );

    // BUG-01 FIX: Track old parent stack during multiple parent changes (Pre->Post event)
    // Multiple rapid parent changes (A→B→C) need a stack to track ALL old parents
    // This allows us to clean up all intermediate parents during Recalc
    private readonly PartitionedSparseRefArray<Stack<PartitionIndex>> _oldParentStack = new(
        Constants.MaxGlobalEntities,
        Constants.MaxSceneEntities
    );

    // Reverse lookup: child entity index -> parent entity index
    // Enables O(1) child removal on deactivation instead of O(n) parent scan
    private readonly PartitionedSparseArray<PartitionIndex> _childToParentLookup = new(
        Constants.MaxGlobalEntities,
        Constants.MaxSceneEntities
    );

    /// <summary>
    /// Tracks a child entity to the specified parent.
    /// Parent and child must be in the same partition (both global or both scene).
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The child entity to add.</param>
    public void TrackChild(Entity parent, Entity child)
    {
        // Both must be global or both must be scene - use TryMatch to extract partition type
        if (parent.Index.TryMatch(out ushort parentGlobal))
        {
            // Parent is global - child must also be global
            if (!child.Index.TryMatch(out ushort childGlobal))
            {
                // Cannot create parent-child relationship across partitions
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    "Cannot create parent-child relationship across partitions (global/scene)"
                ));
                return;
            }

            // Both are global
            if (!_globalParentToChildren.HasValue(parentGlobal))
            {
                _globalParentToChildren[parentGlobal] = new SparseArray<bool>(Constants.MaxGlobalEntities);
            }
            _globalParentToChildren[parentGlobal].Set(childGlobal, true);
        }
        else if (parent.Index.TryMatch(out uint parentScene))
        {
            // Parent is scene - child must also be scene
            if (!child.Index.TryMatch(out uint childScene))
            {
                // Cannot create parent-child relationship across partitions
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    "Cannot create parent-child relationship across partitions (global/scene)"
                ));
                return;
            }

            // Both are scene
            if (!_sceneParentToChildren.HasValue(parentScene))
            {
                _sceneParentToChildren[parentScene] = new SparseArray<bool>(Constants.MaxSceneEntities);
            }
            _sceneParentToChildren[parentScene].Set(childScene, true);
        }

        _childToParentLookup.Set(child.Index, parent.Index);
    }

    /// <summary>
    /// Removes tracking of a child entity from the specified parent.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The child entity to remove.</param>
    public void UntrackChild(Entity parent, Entity child)
    {
        if (parent.Index.TryMatch(out ushort parentGlobal))
        {
            if (_globalParentToChildren.TryGetValue(parentGlobal, out var children))
            {
                if (!child.Index.TryMatch(out ushort childGlobal))
                {
                    // Child must be in same partition as parent
                    EventBus<ErrorEvent>.Push(new ErrorEvent(
                        "Child must be in same partition as parent"
                    ));
                    return;
                }

                children.Remove(childGlobal);
            }
        }
        else if (parent.Index.TryMatch(out uint parentScene))
        {
            if (_sceneParentToChildren.TryGetValue(parentScene, out var children))
            {
                if (!child.Index.TryMatch(out uint childScene))
                {
                    // Child must be in same partition as parent
                    EventBus<ErrorEvent>.Push(new ErrorEvent(
                        "Child must be in same partition as parent"
                    ));
                    return;
                }

                children.Remove(childScene);
            }
        }

        _childToParentLookup.Remove(child.Index);
    }

    /// <summary>
    /// Retrieves all child entities of the specified parent.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <returns>An enumerable of child entities belonging to the parent.</returns>
    public IEnumerable<Entity> GetChildren(Entity parent)
    {
        if (!TryGetChildrenArray(parent.Index, out var children))
        {
            yield break;
        }

        // Use parent's partition to determine how to construct child entities
        if (parent.IsGlobal())
        {
            foreach (var (childIdx, _) in children)
            {
                yield return EntityRegistry.Instance[(ushort)childIdx];
            }
        }
        else
        {
            foreach (var (childIdx, _) in children)
            {
                yield return EntityRegistry.Instance[(uint)childIdx];
            }
        }
    }

    /// <summary>
    /// Tries to get the children array for the specified parent.
    /// </summary>
    /// <param name="parentIndex">The parent entity index.</param>
    /// <param name="children">The sparse array of child indices, if found.</param>
    /// <returns>True if the parent has children; otherwise, false.</returns>
    public bool TryGetChildrenArray(
        PartitionIndex parentIndex,
        [NotNullWhen(true)]
        out SparseArray<bool>? children
    )
    {
        if (parentIndex.TryMatch(out ushort parentGlobal))
        {
            return _globalParentToChildren.TryGetValue(parentGlobal, out children);
        }

        if (parentIndex.TryMatch(out uint parentScene))
        {
            return _sceneParentToChildren.TryGetValue(parentScene, out children);
        }

        children = null;
        return false;
    }

    /// <summary>
    /// Checks whether the specified child entity belongs to the given parent.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The child entity to check for.</param>
    /// <returns>True if the child belongs to the parent; otherwise, false.</returns>
    public bool HasChild(Entity parent, Entity child)
    {
        if (!TryGetChildrenArray(parent.Index, out var children))
        {
            return false;
        }

        // Extract the raw index from child based on partition - use TryMatch
        if (child.Index.TryMatch(out ushort childGlobal))
        {
            return children.HasValue(childGlobal);
        }

        if (child.Index.TryMatch(out uint childScene))
        {
            return children.HasValue(childScene);
        }

        return false;
    }

    /// <summary>
    /// Checks whether the parent has any children.
    /// </summary>
    public bool HasAnyChildren(Entity parent)
    {
        if (!TryGetChildrenArray(parent.Index, out var children))
        {
            return false;
        }

        return children.Count > 0;
    }

    /// <summary>
    /// Recalculates all dirty parent-child relationships.
    /// MUST be called AFTER SelectorRegistry.Recalc() to ensure parent selectors have valid matches.
    /// Typically called automatically by SceneLoader after scene loading.
    /// </summary>
    /// <remarks>
    /// Processes all entities marked dirty since the last Recalc:
    /// - Resolves parent selectors to establish parent-child relationships
    /// - Cleans up old parent relationships when parents change
    /// - Fires ErrorEvent for unresolved parent selectors
    /// </remarks>
    public void Recalc()
    {
        // Process all dirty children and resolve their parent selectors
        // This is called AFTER SelectorRegistry.Recalc() so selectors have updated Matches

        // Process global partition
        foreach (var (childIndex, _) in _dirtyChildren.Global)
        {
            ProcessDirtyChild((ushort)childIndex);
        }

        // Process scene partition
        foreach (var (childIndex, _) in _dirtyChildren.Scene)
        {
            ProcessDirtyChild((uint)childIndex);
        }

        // Clear dirty flags for next frame
        _dirtyChildren.Global.Clear();
        _dirtyChildren.Scene.Clear();
    }

    private void ProcessDirtyChild(PartitionIndex childIndex)
    {
        var child = EntityRegistry.Instance[childIndex];

        // Handle race condition: entity deactivated between dirty mark and Recalc
        if (!child.Active)
        {
            // BUG-01 FIX: Clear entire stack of old parents
            if (_oldParentStack.TryGetValue(childIndex, out var stack))
            {
                stack.Clear();
                _oldParentStack.Remove(childIndex);
            }
            return;
        }

        // Get the child's ParentBehavior (may have been removed)
        if (!child.TryGetBehavior<ParentBehavior>(out var behavior))
        {
            // BUG-01 FIX: ParentBehavior was removed - clean up ALL old parents from stack
            if (_oldParentStack.TryGetValue(childIndex, out var stack))
            {
                while (stack.Count > 0)
                {
                    var oldParentIdx = stack.Pop();
                    UntrackChild(EntityRegistry.Instance[oldParentIdx], child);
                }
                _oldParentStack.Remove(childIndex);
            }
            return;
        }

        // Try to resolve the new parent using the selector
        var hasNewParent = behavior.Value.TryFindParent(child.IsGlobal(), out var newParent);

        // Fire error event if parent selector doesn't resolve
        if (!hasNewParent && child.TryGetBehavior<IdBehavior>(out var idBehavior))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unresolved parent reference for entity '{idBehavior.Value.Id}'"
            ));
        }

        // BUG-01 FIX: Clean up ALL old parent relationships from stack
        if (_oldParentStack.TryGetValue(childIndex, out var oldParentStack))
        {
            // Process all old parents in the stack
            while (oldParentStack.Count > 0)
            {
                var oldParentIdx = oldParentStack.Pop();

                // Only untrack if this parent is different from the new one
                if (!hasNewParent || newParent!.Value.Index != oldParentIdx)
                {
                    UntrackChild(EntityRegistry.Instance[oldParentIdx], child);
                }
            }
            _oldParentStack.Remove(childIndex);
        }

        // Track new parent relationship if one was found
        if (hasNewParent)
        {
            TrackChild(newParent!.Value, child);
        }
    }

    public void OnEvent(BehaviorAddedEvent<ParentBehavior> e)
    {
        // Mark child as dirty instead of immediately tracking
        // Parent relationship will be established during Recalc()
        _dirtyChildren.Set(e.Entity.Index, true);
    }

    public void OnEvent(PreBehaviorUpdatedEvent<ParentBehavior> e)
    {
        // BUG-01 FIX: Push old parent onto stack instead of replacing lookup
        // This allows tracking multiple rapid parent changes (A→B→C)
        if (BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(e.Entity, out var oldBehavior))
        {
            if (oldBehavior.Value.TryFindParent(e.Entity.IsGlobal(), out var oldParent))
            {
                if (!_oldParentStack.HasValue(e.Entity.Index))
                {
                    _oldParentStack[e.Entity.Index] = new Stack<PartitionIndex>();
                }
                _oldParentStack[e.Entity.Index].Push(oldParent.Value.Index);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<ParentBehavior> e)
    {
        // Mark child as dirty, Recalc will handle old parent cleanup
        _dirtyChildren.Set(e.Entity.Index, true);
    }

    public void OnEvent(PreBehaviorRemovedEvent<ParentBehavior> e)
    {
        // BUG-01 FIX: Push old parent onto stack, mark dirty so Recalc processes removal
        if (BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(e.Entity, out var behavior))
        {
            if (behavior.Value.TryFindParent(e.Entity.IsGlobal(), out var parent))
            {
                if (!_oldParentStack.HasValue(e.Entity.Index))
                {
                    _oldParentStack[e.Entity.Index] = new Stack<PartitionIndex>();
                }
                _oldParentStack[e.Entity.Index].Push(parent.Value.Index);
            }
        }
        _dirtyChildren.Set(e.Entity.Index, true);
    }

    public void OnEvent(PreEntityDeactivatedEvent e)
    {
        // Clean up both parent-child tracking and dirty state on deactivation
        _dirtyChildren.Remove(e.Entity.Index);

        // BUG-01 FIX: Clear stack of old parents
        if (_oldParentStack.TryGetValue(e.Entity.Index, out var stack))
        {
            stack.Clear();
            _oldParentStack.Remove(e.Entity.Index);
        }

        // If this entity is a child, remove it from its parent's child list (O(1) lookup)
        if (_childToParentLookup.TryGetValue(e.Entity.Index, out var parentIdx))
        {
            UntrackChild(EntityRegistry.Instance[parentIdx.Value], e.Entity);
        }

        // If this entity is a parent, orphan all its children
        if (!TryGetChildrenArray(e.Entity.Index, out var ownChildren))
        {
            return;
        }

        // Remove ParentBehavior from all children
        // Note: TryGetChildrenArray returns a snapshot, safe to iterate and modify
        foreach (var (childIdx, _) in ownChildren)
        {
            // Use IsGlobal to determine partition
            Entity child = e.Entity.IsGlobal()
                ? EntityRegistry.Instance[(ushort)childIdx]
                : EntityRegistry.Instance[(uint)childIdx];

            // Only remove if child is still active
            if (child.Active)
            {
                child.RemoveBehavior<ParentBehavior>();
            }
        }
    }

    /// <summary>
    /// Resets scene partition by recalculating hierarchy after scene entities cleared.
    /// Called by ResetDriver during scene transitions.
    /// </summary>
    public void Reset()
    {
        // Recalc to ensure hierarchy is in clean state after scene entities deactivated
        Recalc();
    }

    public void OnEvent(InitializeEvent e)
    {
        EventBus<BehaviorAddedEvent<ParentBehavior>>.Register(this);
        EventBus<PreBehaviorUpdatedEvent<ParentBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<ParentBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<ParentBehavior>>.Register(this);
        EventBus<PreEntityDeactivatedEvent>.Register(this);
        EventBus<ShutdownEvent>.Register(this);
    }

    public void OnEvent(ShutdownEvent _)
    {
        EventBus<BehaviorAddedEvent<ParentBehavior>>.Unregister(this);
        EventBus<PreBehaviorUpdatedEvent<ParentBehavior>>.Unregister(this);
        EventBus<PostBehaviorUpdatedEvent<ParentBehavior>>.Unregister(this);
        EventBus<PreBehaviorRemovedEvent<ParentBehavior>>.Unregister(this);
        EventBus<PreEntityDeactivatedEvent>.Unregister(this);
        EventBus<ShutdownEvent>.Unregister(this);
    }
}
