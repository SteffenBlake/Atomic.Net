using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED.Hierarchy;

/// <summary>
/// Tracks parent-to-child relationships between entities using a fixed-size jagged boolean array.
/// This registry does not store any other state and serves as a fast lookup for hierarchical queries.
/// </summary>
public class HierarchyRegistry : 
    ISingleton<HierarchyRegistry>,
    IEventHandler<BehaviorAddedEvent<Parent>>,
    IEventHandler<PreBehaviorUpdatedEvent<Parent>>,
    IEventHandler<PostBehaviorUpdatedEvent<Parent>>,
    IEventHandler<PreBehaviorRemovedEvent<Parent>>,
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

    public void OnEvent(BehaviorAddedEvent<Parent> e)
    {
        if (BehaviorRegistry<Parent>.Instance.TryGetBehavior(e.Entity, out var behavior))
        {
            TrackChild(new Entity(behavior.Value.ParentIndex), e.Entity);
        }
    }

    public void OnEvent(PreBehaviorUpdatedEvent<Parent> e)
    {
        if (BehaviorRegistry<Parent>.Instance.TryGetBehavior(e.Entity, out var oldBehavior))
        {
            UntrackChild(
                EntityRegistry.Instance[oldBehavior.Value.ParentIndex], 
                e.Entity
            );
        }
    }

    public void OnEvent(PostBehaviorUpdatedEvent<Parent> e)
    {
        if (BehaviorRegistry<Parent>.Instance.TryGetBehavior(e.Entity, out var newBehavior))
        {
            TrackChild(
                EntityRegistry.Instance[newBehavior.Value.ParentIndex], 
                e.Entity
            );
        }
    }

    public void OnEvent(PreBehaviorRemovedEvent<Parent> e)
    {
        if (BehaviorRegistry<Parent>.Instance.TryGetBehavior(e.Entity, out var behavior))
        {
            UntrackChild(
                EntityRegistry.Instance[behavior.Value.ParentIndex], 
                e.Entity
            );
        }
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
            BehaviorRegistry<Parent>.Instance.Remove(child);
        }
    }

    public void OnEvent(InitializeEvent e)
    {
        EventBus<BehaviorAddedEvent<Parent>>.Register<HierarchyRegistry>();
        EventBus<BehaviorAddedEvent<Parent>>.Register<HierarchyRegistry>();
        EventBus<PreBehaviorUpdatedEvent<Parent>>.Register<HierarchyRegistry>();
        EventBus<PostBehaviorUpdatedEvent<Parent>>.Register<HierarchyRegistry>();
        EventBus<PreBehaviorRemovedEvent<Parent>>.Register<HierarchyRegistry>();
        EventBus<PreEntityDeactivatedEvent>.Register<HierarchyRegistry>();
    }
}
