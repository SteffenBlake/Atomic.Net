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
        = new((int)Constants.MaxSceneEntities);

    // Track dirty children who need parent resolution during Recalc
    private readonly PartitionedSparseArray<bool> _dirtyChildren = new(
        Constants.MaxGlobalEntities,
        Constants.MaxSceneEntities
    );
    
    // Track old parent index during parent change (Pre->Post event)
    // This allows us to clean up the old parent's child list during Recalc
    private readonly PartitionedSparseArray<PartitionIndex> _oldParentLookup = new(
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
        // Both must be global or both must be scene
        // Use Visit to extract partition type and value safely
        var parentGlobal = parent.Index.Visit(
            static global => (ushort?)global,
            static scene => (ushort?)null,
            static () => (ushort?)null
        );
        var childGlobal = child.Index.Visit(
            static global => (ushort?)global,
            static scene => (ushort?)null,
            static () => (ushort?)null
        );
        
        var parentIsGlobal = parentGlobal.HasValue;
        var childIsGlobal = childGlobal.HasValue;
        
        if (parentIsGlobal != childIsGlobal)
        {
            // Cannot create parent-child relationship across partitions
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Cannot create parent-child relationship across partitions (global/scene)"
            ));
            return;
        }

        if (parentIsGlobal)
        {
            // Both are global
            if (parentGlobal.HasValue && childGlobal.HasValue)
            {
                if (!_globalParentToChildren.HasValue(parentGlobal.Value))
                {
                    _globalParentToChildren[parentGlobal.Value] = new SparseArray<bool>(Constants.MaxGlobalEntities);
                }
                _globalParentToChildren[parentGlobal.Value].Set(childGlobal.Value, true);
            }
        }
        else
        {
            // Both are scene
            var parentScene = parent.Index.Visit(
                static _ => throw new InvalidOperationException(),
                static scene => (ushort)scene,
                static () => throw new InvalidOperationException()
            );
            var childScene = child.Index.Visit(
                static _ => throw new InvalidOperationException(),
                static scene => (ushort)scene,
                static () => throw new InvalidOperationException()
            );
            
            if (!_sceneParentToChildren.HasValue(parentScene))
            {
                _sceneParentToChildren[parentScene] = new SparseArray<bool>((int)Constants.MaxSceneEntities);
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
        var parentGlobal = parent.Index.Visit(
            static global => (ushort?)global,
            static scene => (ushort?)null,
            static () => (ushort?)null
        );
        
        if (parentGlobal.HasValue)
        {
            if (_globalParentToChildren.TryGetValue(parentGlobal.Value, out var children))
            {
                var childGlobal = child.Index.Visit(
                    static global => global,
                    static _ => throw new InvalidOperationException("Child must be in same partition as parent"),
                    static () => throw new InvalidOperationException()
                );
                
                _ = children.Remove(childGlobal);
            }
        }
        else
        {
            var parentScene = parent.Index.Visit(
                static _ => throw new InvalidOperationException(),
                static scene => (ushort)scene,
                static () => throw new InvalidOperationException()
            );
            
            if (_sceneParentToChildren.TryGetValue(parentScene, out var children))
            {
                var childScene = child.Index.Visit(
                    static _ => throw new InvalidOperationException("Child must be in same partition as parent"),
                    static scene => (ushort)scene,
                    static () => throw new InvalidOperationException()
                );
                
                _ = children.Remove(childScene);
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
        var parentGlobal = parent.Index.Visit(
            static global => (ushort?)global,
            static scene => (ushort?)null,
            static () => (ushort?)null
        );
        
        if (parentGlobal.HasValue)
        {
            if (_globalParentToChildren.TryGetValue(parentGlobal.Value, out var children))
            {
                foreach (var (childIdx, _) in children)
                {
                    yield return EntityRegistry.Instance[(ushort)childIdx];
                }
            }
        }
        else
        {
            var parentScene = parent.Index.Visit(
                static _ => throw new InvalidOperationException(),
                static scene => (ushort)scene,
                static () => throw new InvalidOperationException()
            );
            
            if (_sceneParentToChildren.TryGetValue(parentScene, out var children))
            {
                foreach (var (childIdx, _) in children)
                {
                    yield return EntityRegistry.Instance[(uint)childIdx];
                }
            }
        }
    }

    /// <summary>
    /// Gets the child sparse array for a parent, or null if no children exist.
    /// Allows allocation-free iteration of child indices.
    /// </summary>
    /// <param name="parentIndex">The parent entity index.</param>
    /// <returns>The sparse array of children, or null if no children.</returns>
    public SparseArray<bool>? GetChildrenArray(PartitionIndex parentIndex)
    {
        var parentGlobal = parentIndex.Visit(
            static global => (ushort?)global,
            static scene => (ushort?)null,
            static () => (ushort?)null
        );
        
        if (parentGlobal.HasValue)
        {
            return _globalParentToChildren.TryGetValue(parentGlobal.Value, out var children) 
                ? children : null;
        }
        
        var parentScene = parentIndex.Visit(
            static global => (ushort?)null,
            static scene => (ushort?)scene,
            static () => (ushort?)null
        );
        
        if (parentScene.HasValue)
        {
            return _sceneParentToChildren.TryGetValue(parentScene.Value, out var children)
                ? children : null;
        }
        
        return null;
    }

    /// <summary>
    /// Checks whether the specified child entity belongs to the given parent.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The child entity to check for.</param>
    /// <returns>True if the child belongs to the parent; otherwise, false.</returns>
    public bool HasChild(Entity parent, Entity child)
    {
        var children = GetChildrenArray(parent.Index);
        if (children == null)
        {
            return false;
        }
        
        // Extract the raw index from child based on partition
        var childGlobal = child.Index.Visit(
            static global => (ushort?)global,
            static scene => (ushort?)null,
            static () => (ushort?)null
        );
        if (childGlobal.HasValue)
        {
            return children.HasValue(childGlobal.Value);
        }
        
        var childScene = child.Index.Visit(
            static global => (ushort?)null,
            static scene => (ushort?)scene,
            static () => (ushort?)null
        );
        if (childScene.HasValue)
        {
            return children.HasValue(childScene.Value);
        }
        return false;
    }

    /// <summary>
    /// Checks whether the parent has any children.
    /// </summary>
    public bool HasAnyChildren(Entity parent)
    {
        var children = GetChildrenArray(parent.Index);
        return children != null && children.Count > 0;
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
            _oldParentLookup.Remove(childIndex);
            return;
        }
        
        // Get the child's ParentBehavior (may have been removed)
        if (!BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(child, out var behavior))
        {
            // ParentBehavior was removed - clean up old parent if we tracked one
            if (_oldParentLookup.TryGetValue(childIndex, out var oldParentIdx) && oldParentIdx.HasValue)
            {
                UntrackChild(EntityRegistry.Instance[oldParentIdx.Value], child);
                _oldParentLookup.Remove(childIndex);
            }
            return;
        }
        
        // Try to resolve the new parent using the selector
        var hasNewParent = behavior.Value.TryFindParent(child, out var newParent);
        
        // Fire error event if parent selector doesn't resolve
        if (!hasNewParent && child.TryGetBehavior<IdBehavior>(out var idBehavior))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unresolved parent reference for entity '{idBehavior.Value.Id}'"
            ));
        }
        
        // Clean up old parent relationship if one was tracked
        if (_oldParentLookup.TryGetValue(childIndex, out var oldParentIndex) && oldParentIndex.HasValue)
        {
            var oldParent = EntityRegistry.Instance[oldParentIndex.Value];
            
            // Only untrack/retrack if the parent actually changed
            if (!hasNewParent || newParent!.Value.Index != oldParentIndex.Value)
            {
                UntrackChild(oldParent, child);
                
                // Track new parent relationship if one was found
                if (hasNewParent)
                {
                    TrackChild(newParent!.Value, child);
                }
            }
            // else: parent didn't change, no need to untrack/retrack
            
            _oldParentLookup.Remove(childIndex);
        }
        else
        {
            // No old parent tracked, just track the new one
            if (hasNewParent)
            {
                TrackChild(newParent!.Value, child);
            }
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
        // Store old parent index for cleanup during Recalc
        if (BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(e.Entity, out var oldBehavior))
        {
            if (oldBehavior.Value.TryFindParent(e.Entity, out var oldParent))
            {
                _oldParentLookup.Set(e.Entity.Index, oldParent.Value.Index);
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
        // Store old parent for cleanup, mark dirty so Recalc processes removal
        if (BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(e.Entity, out var behavior))
        {
            if (behavior.Value.TryFindParent(e.Entity, out var parent))
            {
                _oldParentLookup.Set(e.Entity.Index, parent.Value.Index);
            }
        }
        _dirtyChildren.Set(e.Entity.Index, true);
    }

    public void OnEvent(PreEntityDeactivatedEvent e)
    {
        // Clean up both parent-child tracking and dirty state on deactivation
        _dirtyChildren.Remove(e.Entity.Index);
        _oldParentLookup.Remove(e.Entity.Index);
        
        // If this entity is a child, remove it from its parent's child list (O(1) lookup)
        if (_childToParentLookup.TryGetValue(e.Entity.Index, out var parentIdx) && parentIdx.HasValue)
        {
            UntrackChild(EntityRegistry.Instance[parentIdx.Value], e.Entity);
        }
        
        // If this entity is a parent, orphan all its children
        var ownChildren = GetChildrenArray(e.Entity.Index);
        if (ownChildren == null)
        {
            return;
        }

        // Remove ParentBehavior from all children
        // Note: GetChildrenArray returns a snapshot, safe to iterate and modify
        foreach (var (childIdx, _) in ownChildren)
        {
            // Use TryMatch to avoid closure allocation
            Entity child;
            if (e.Entity.Index.TryMatch(out ushort globalIdx))
            {
                child = EntityRegistry.Instance[(ushort)childIdx];
            }
            else if (e.Entity.Index.TryMatch(out uint sceneIdx))
            {
                child = EntityRegistry.Instance[(uint)childIdx];
            }
            else
            {
                throw new InvalidOperationException("Invalid PartitionIndex state");
            }
            
            // Only remove if child is still active
            if (child.Active)
            {
                BehaviorRegistry<ParentBehavior>.Instance.Remove(child);
            }
        }
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
