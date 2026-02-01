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
        var parentIsGlobal = parent.Index.TryMatch(out ushort? parentGlobal);
        var childIsGlobal = child.Index.TryMatch(out ushort? childGlobal);
        
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
            if (!_globalParentToChildren.HasValue(parentGlobal!.Value))
            {
                _globalParentToChildren[parentGlobal.Value] = new SparseArray<bool>(Constants.MaxGlobalEntities);
            }
            _globalParentToChildren[parentGlobal.Value].Set(childGlobal!.Value, true);
        }
        else
        {
            // Both are scene
            var parentScene = parent.Index.Visit(
                _ => throw new InvalidOperationException(),
                scene => (ushort)scene,
                () => throw new InvalidOperationException()
            );
            var childScene = child.Index.Visit(
                _ => throw new InvalidOperationException(),
                scene => (ushort)scene,
                () => throw new InvalidOperationException()
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
        var parentIsGlobal = parent.Index.TryMatch(out ushort? parentGlobal);
        
        if (parentIsGlobal)
        {
            if (!_globalParentToChildren.TryGetValue(parentGlobal!.Value, out var children))
            {
                return;
            }
            
            var childGlobal = child.Index.Visit(
                global => global,
                _ => throw new InvalidOperationException("Child must be in same partition as parent"),
                () => throw new InvalidOperationException()
            );
            
            _ = children.Remove(childGlobal);
        }
        else
        {
            var parentScene = parent.Index.Visit(
                _ => throw new InvalidOperationException(),
                scene => (ushort)scene,
                () => throw new InvalidOperationException()
            );
            
            if (!_sceneParentToChildren.TryGetValue(parentScene, out var children))
            {
                return;
            }
            
            var childScene = child.Index.Visit(
                _ => throw new InvalidOperationException("Child must be in same partition as parent"),
                scene => (ushort)scene,
                () => throw new InvalidOperationException()
            );
            
            _ = children.Remove(childScene);
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
        var parentIsGlobal = parent.Index.TryMatch(out ushort? parentGlobal);
        
        if (parentIsGlobal)
        {
            if (!_globalParentToChildren.TryGetValue(parentGlobal!.Value, out var children))
            {
                yield break;
            }

            foreach (var (childIdx, _) in children)
            {
                yield return EntityRegistry.Instance[(ushort)childIdx];
            }
        }
        else
        {
            var parentScene = parent.Index.Visit(
                _ => throw new InvalidOperationException(),
                scene => (ushort)scene,
                () => throw new InvalidOperationException()
            );
            
            if (!_sceneParentToChildren.TryGetValue(parentScene, out var children))
            {
                yield break;
            }

            foreach (var (childIdx, _) in children)
            {
                yield return EntityRegistry.Instance[(uint)childIdx];
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
        if (parentIndex.TryMatch(out ushort? parentGlobal))
        {
            return _globalParentToChildren.TryGetValue(parentGlobal.Value, out var children) 
                ? children : null;
        }
        
        if (parentIndex.TryMatch(out uint? parentScene))
        {
            return _sceneParentToChildren.TryGetValue((ushort)parentScene.Value, out var children)
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
        if (child.Index.TryMatch(out ushort? childGlobal))
        {
            return children.HasValue(childGlobal.Value);
        }
        if (child.Index.TryMatch(out uint? childScene))
        {
            return children.HasValue((ushort)childScene.Value);
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
            if (_oldParentLookup.TryGetValue(childIndex, out var oldParentIdx))
            {
                UntrackChild(EntityRegistry.Instance[oldParentIdx], child);
                _oldParentLookup.Remove(childIndex);
            }
            return;
        }
        
        // Try to resolve the new parent using the selector
        var hasNewParent = behavior.Value.TryFindParent(out var newParent);
        
        // Fire error event if parent selector doesn't resolve
        if (!hasNewParent && child.TryGetBehavior<IdBehavior>(out var idBehavior))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unresolved parent reference for entity '{idBehavior.Value.Id}'"
            ));
        }
        
        // Clean up old parent relationship if one was tracked
        if (_oldParentLookup.TryGetValue(childIndex, out var oldParentIndex))
        {
            var oldParent = EntityRegistry.Instance[oldParentIndex];
            
            // Only untrack/retrack if the parent actually changed
            if (!hasNewParent || newParent!.Value.Index != oldParentIndex)
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
            if (oldBehavior.Value.TryFindParent(out var oldParent))
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
            if (behavior.Value.TryFindParent(out var parent))
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
        if (_childToParentLookup.TryGetValue(e.Entity.Index, out var parentIdx))
        {
            UntrackChild(EntityRegistry.Instance[parentIdx.Value], e.Entity);
        }
        
        // If this entity is a parent, orphan all its children
        if (!_parentToChildLookup.TryGetValue(e.Entity.Index, out var ownChildren))
        {
            return;
        }

        while (ownChildren.TryPop(out _, out var childIndex))
        {
            var child = EntityRegistry.Instance[childIndex.Value];
            BehaviorRegistry<ParentBehavior>.Instance.Remove(child);
        }
        
        // Clean up the parent's child list to free memory
        _parentToChildLookup.Remove(e.Entity.Index);
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
