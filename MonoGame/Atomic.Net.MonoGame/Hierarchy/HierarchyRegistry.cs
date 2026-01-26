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

    private readonly SparseReferenceArray<SparseArray<bool>> _parentToChildLookup 
        = new(Constants.MaxEntities);

    // senior-dev: Track dirty children who need parent resolution during Recalc
    private readonly SparseArray<bool> _dirtyChildren = new(Constants.MaxEntities);
    
    // senior-dev: Track old parent index during parent change (Pre->Post event)
    // This allows us to clean up the old parent's child list during Recalc
    private readonly SparseArray<ushort> _oldParentIndex = new(Constants.MaxEntities);

    /// <summary>
    /// Tracks a child entity to the specified parent.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The child entity to add.</param>
    public void TrackChild(Entity parent, Entity child)
    {
        if (!_parentToChildLookup.HasValue(parent.Index))
        {
            _parentToChildLookup[parent.Index] = new SparseArray<bool>(Constants.MaxEntities);
        }

        _parentToChildLookup[parent.Index].Set(child.Index, true);
    }

    /// <summary>
    /// Removes tracking of a child entity from the specified parent.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The child entity to remove.</param>
    public void UntrackChild(Entity parent, Entity child)
    {
        if (!_parentToChildLookup.TryGetValue(parent.Index, out var children))
        {
            return;
        }

        _ = children.Remove(child.Index);
    }

    /// <summary>
    /// Retrieves all child entities of the specified parent.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <returns>An enumerable of child entities belonging to the parent.</returns>
    public IEnumerable<Entity> GetChildren(Entity parent)
    {
        if (!_parentToChildLookup.TryGetValue(parent.Index, out var children))
        {
            yield break;
        }

        foreach (var (childIdx, _) in children)
        {
            yield return EntityRegistry.Instance[childIdx];
        }
    }

    /// <summary>
    /// Gets the child sparse array for a parent, or null if no children exist.
    /// Allows allocation-free iteration of child indices.
    /// </summary>
    /// <param name="parentIndex">The parent entity index.</param>
    /// <returns>The sparse array of children, or null if no children.</returns>
    public SparseArray<bool>? GetChildrenArray(ushort parentIndex)
    {
        return _parentToChildLookup.TryGetValue(parentIndex, out var children) ? children : null;
    }

    /// <summary>
    /// Checks whether the specified child entity belongs to the given parent.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The child entity to check for.</param>
    /// <returns>True if the child belongs to the parent; otherwise, false.</returns>
    public bool HasChild(Entity parent, Entity child)
    {
        return _parentToChildLookup.TryGetValue(parent.Index, out var children) &&
            children.HasValue(child.Index);
    }

    /// <summary>
    /// Checks whether the parent has any children.
    /// </summary>
    public bool HasAnyChildren(Entity parent)
    {
        return _parentToChildLookup.TryGetValue(parent.Index, out var children) &&
            children.Count > 0;
    }

    /// <summary>
    /// Recalculates all dirty parent-child relationships.
    /// This must be called AFTER SelectorRegistry.Recalc() to ensure selectors are resolved.
    /// Processes all children marked dirty since last Recalc and updates their parent relationships.
    /// </summary>
    public void Recalc()
    {
        // senior-dev: Process all dirty children and resolve their parent selectors
        // This is called AFTER SelectorRegistry.Recalc() so selectors have updated Matches
        foreach (var (childIndex, _) in _dirtyChildren)
        {
            var child = EntityRegistry.Instance[childIndex];
            
            // Skip inactive entities (deactivated between dirty mark and Recalc)
            if (!child.Active)
            {
                _oldParentIndex.Remove(childIndex);
                continue;
            }
            
            // Get the child's ParentBehavior (may have been removed)
            if (!BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(child, out var behavior))
            {
                // ParentBehavior was removed - clean up old parent if we tracked one
                if (_oldParentIndex.TryGetValue(childIndex, out var oldParentIdx))
                {
                    UntrackChild(EntityRegistry.Instance[oldParentIdx.Value], child);
                    _oldParentIndex.Remove(childIndex);
                }
                continue;
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
            if (_oldParentIndex.TryGetValue(childIndex, out var oldParentIndex))
            {
                var oldParent = EntityRegistry.Instance[oldParentIndex.Value];
                
                // Only untrack if the parent actually changed
                if (!hasNewParent || newParent!.Value.Index != oldParentIndex.Value)
                {
                    UntrackChild(oldParent, child);
                }
                
                _oldParentIndex.Remove(childIndex);
            }
            
            // Track new parent relationship if one was found
            if (hasNewParent)
            {
                TrackChild(newParent!.Value, child);
            }
        }
        
        // Clear dirty flags for next frame
        _dirtyChildren.Clear();
    }

    public void OnEvent(BehaviorAddedEvent<ParentBehavior> e)
    {
        // senior-dev: Mark child as dirty instead of immediately tracking
        // Parent relationship will be established during Recalc()
        _dirtyChildren.Set(e.Entity.Index, true);
    }

    public void OnEvent(PreBehaviorUpdatedEvent<ParentBehavior> e)
    {
        // senior-dev: Store old parent index for cleanup during Recalc
        if (BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(e.Entity, out var oldBehavior))
        {
            if (oldBehavior.Value.TryFindParent(out var oldParent))
            {
                _oldParentIndex.Set(e.Entity.Index, oldParent.Value.Index);
            }
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<ParentBehavior> e)
    {
        // senior-dev: Mark child as dirty, Recalc will handle old parent cleanup
        _dirtyChildren.Set(e.Entity.Index, true);
    }

    public void OnEvent(PreBehaviorRemovedEvent<ParentBehavior> e)
    {
        // senior-dev: Store old parent for cleanup, mark dirty so Recalc processes removal
        if (BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(e.Entity, out var behavior))
        {
            if (behavior.Value.TryFindParent(out var parent))
            {
                _oldParentIndex.Set(e.Entity.Index, parent.Value.Index);
            }
        }
        _dirtyChildren.Set(e.Entity.Index, true);
    }

    public void OnEvent(PreEntityDeactivatedEvent e)
    {
        // senior-dev: Clean up both parent-child tracking and dirty state on deactivation
        // Remove from dirty children list to prevent processing deactivated entities
        _dirtyChildren.Remove(e.Entity.Index);
        _oldParentIndex.Remove(e.Entity.Index);
        
        // If this entity is a child, remove it from its parent's child list
        // We need to scan all parents to find which one has this child
        // (This is rare - only happens on deactivation, not hot path)
        foreach (var (parentIndex, children) in _parentToChildLookup)
        {
            if (children.HasValue(e.Entity.Index))
            {
                children.Remove(e.Entity.Index);
            }
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
    }

    public void OnEvent(InitializeEvent e)
    {
        // senior-dev: Fixed duplicate registration of BehaviorAddedEvent (was registered twice)
        EventBus<BehaviorAddedEvent<ParentBehavior>>.Register(this);
        EventBus<PreBehaviorUpdatedEvent<ParentBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<ParentBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<ParentBehavior>>.Register(this);
        EventBus<PreEntityDeactivatedEvent>.Register(this);
        EventBus<ShutdownEvent>.Register(this);
    }

    public void OnEvent(ShutdownEvent _)
    {
        // senior-dev: Unregister from all events to prevent duplicate registrations
        EventBus<BehaviorAddedEvent<ParentBehavior>>.Unregister(this);
        EventBus<PreBehaviorUpdatedEvent<ParentBehavior>>.Unregister(this);
        EventBus<PostBehaviorUpdatedEvent<ParentBehavior>>.Unregister(this);
        EventBus<PreBehaviorRemovedEvent<ParentBehavior>>.Unregister(this);
        EventBus<PreEntityDeactivatedEvent>.Unregister(this);
        EventBus<ShutdownEvent>.Unregister(this);
    }
}
