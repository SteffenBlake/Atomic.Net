using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Selectors;

namespace Atomic.Net.MonoGame.Hierarchy;

/// <summary>
/// Provides extension methods for <see cref="Entity"/> to manage parent-child relationships.
/// </summary>
public static class HierarchyEntityExtensions
{
    /// <summary>
    /// Adds a child entity to this parent.
    /// Internally sets the child's <see cref="ParentBehavior"/> behavior to this entity.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The child entity to add.</param>
    /// <returns>The parent entity for fluent chaining.</returns>
    public static Entity WithChild(this Entity parent, Entity child)
    {
        _ = child.WithParent(parent);
        return parent;
    }

    /// <summary>
    /// Sets the parent of this child entity.
    /// Updates this entity's <see cref="ParentBehavior"/> behavior to reference the specified parent.
    /// </summary>
    /// <param name="child">The child entity.</param>
    /// <param name="parent">The parent entity.</param>
    /// <returns>The child entity for fluent chaining.</returns>
    public static Entity WithParent(this Entity child, Entity parent)
    {
        if (!parent.TryGetBehavior<IdBehavior>(out var idBehavior))
        {
            throw new InvalidOperationException(
                "Attempted to force child to a parent without an IdBehavior"
            );
        }

        if (SelectorRegistry.Instance.TryParse(idBehavior.Value.Id, out var selector))
        {
            child.SetBehavior<ParentBehavior, EntitySelector>(
                in selector,
                static (ref readonly _selector, ref behavior) => behavior = new(_selector)
            );
        }

        return child;
    }

    /// <summary>
    /// Retrieves all children of this parent entity.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <returns>An enumerable of child entities belonging to the parent.</returns>
    public static IEnumerable<Entity> GetChildren(this Entity parent)
    {
        return HierarchyRegistry.Instance.GetChildren(parent);
    }

    /// <summary>
    /// Retrieves the parent entity of this child entity, if any.
    /// </summary>
    /// <param name="child">The child entity.</param>
    /// <returns>true if a parent was retrieved</returns>
    public static bool TryGetParent(
        this Entity child,
        [NotNullWhen(true)]
        out Entity? parent
    )
    {
        if (child.TryGetBehavior<ParentBehavior>(out var parentBehavior))
        {
            return parentBehavior.Value.TryFindParent(child.IsGlobal(), out parent);
        }

        parent = default;
        return false;
    }


    /// <summary>
    /// Retrieves the parent entity of this child entity, if any.
    /// </summary>
    /// <param name="child">The child entity.</param>
    /// <returns>The parent entity, or <c>null</c> if the child has no parent.</returns>
    public static Entity? GetParent(this Entity child)
    {
        if (BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(
            child, out var parentBehavior
        ))
        {
            if (parentBehavior.Value.TryFindParent(child.IsGlobal(), out var parent))
            {
                return parent;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks whether this child entity has a parent assigned.
    /// </summary>
    /// <param name="child">The child entity.</param>
    /// <returns><c>true</c> if the child has a parent; otherwise, <c>false</c>.</returns>
    public static bool HasParent(this Entity child)
    {
        return BehaviorRegistry<ParentBehavior>.Instance.HasBehavior(child);
    }

    /// <summary>
    /// Checks whether this parent entity has the specified child.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The child entity to check.</param>
    /// <returns><c>true</c> if the child belongs to the parent; otherwise, <c>false</c>.</returns>
    public static bool HasChild(this Entity parent, Entity child)
    {
        return HierarchyRegistry.Instance.HasChild(parent, child);
    }

    /// <summary>
    /// Checks whether this child entity has the specified parent.
    /// </summary>
    /// <param name="child">The child entity.</param>
    /// <param name="parent">The parent entity to check.</param>
    /// <returns><c>true</c> if the child's parent matches the specified entity; otherwise, <c>false</c>.</returns>
    public static bool HasParent(this Entity child, Entity parent)
    {
        if (!BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(child, out var parentBehavior))
        {
            return false;
        }

        if (!parentBehavior.Value.TryFindParent(child.IsGlobal(), out var located))
        {
            return false;
        }

        return located.Value.Index == parent.Index;
    }

    /// <summary>
    /// Checks whether this parent entity has any children.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <returns><c>true</c> if the parent has one or more children; otherwise, <c>false</c>.</returns>
    public static bool HasAnyChildren(this Entity parent)
    {
        return HierarchyRegistry.Instance.HasAnyChildren(parent);
    }
}


