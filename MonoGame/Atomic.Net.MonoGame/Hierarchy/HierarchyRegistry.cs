using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

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
    IEventHandler<InitializeEvent>
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

    public void OnEvent(BehaviorAddedEvent<ParentBehavior> e)
    {
        if (!BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(e.Entity, out var behavior))
        {
            return;
        }

        if (!behavior.Value.TryFindParent(out var parent))
        {
            return;
        }

        TrackChild(parent.Value, e.Entity);
    }

    public void OnEvent(PreBehaviorUpdatedEvent<ParentBehavior> e)
    {
        if (!BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(e.Entity, out var oldBehavior))
        {
            return;
        }

        if (!oldBehavior.Value.TryFindParent(out var oldParent))
        {
            return;
        }

        UntrackChild(
            oldParent.Value,
            e.Entity
        );
    }

    public void OnEvent(PostBehaviorUpdatedEvent<ParentBehavior> e)
    {
        if (!BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(e.Entity, out var newBehavior))
        {
            return;
        }

        if (!newBehavior.Value.TryFindParent(out var newParent))
        {
            return;
        }

        TrackChild(
            newParent.Value,
            e.Entity
        );
    }

    public void OnEvent(PreBehaviorRemovedEvent<ParentBehavior> e)
    {
        if (!BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(e.Entity, out var behavior))
        {
            return;
        }

        if (!behavior.Value.TryFindParent(out var parent))
        {
            return;
        }

        UntrackChild(
            parent.Value,
            e.Entity
        );
    }

    public void OnEvent(PreEntityDeactivatedEvent e)
    {
        if (!_parentToChildLookup.TryGetValue(e.Entity.Index, out var children))
        {
            return;
        }

        while (children.TryPop(out _, out var childIndex))
        {
            var child = EntityRegistry.Instance[childIndex.Value];
            BehaviorRegistry<ParentBehavior>.Instance.Remove(child);
        }
    }

    public void OnEvent(InitializeEvent e)
    {
        EventBus<BehaviorAddedEvent<ParentBehavior>>.Register(this);
        EventBus<BehaviorAddedEvent<ParentBehavior>>.Register(this);
        EventBus<PreBehaviorUpdatedEvent<ParentBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<ParentBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<ParentBehavior>>.Register(this);
        EventBus<PreEntityDeactivatedEvent>.Register(this);
    }
}
