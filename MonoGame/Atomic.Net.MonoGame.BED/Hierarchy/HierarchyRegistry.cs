namespace Atomic.Net.MonoGame.BED.Hierarchy;

/// <summary>
/// Tracks parent-to-child relationships between entities using a fixed-size jagged boolean array.
/// This registry does not store any other state and serves as a fast lookup for hierarchical queries.
/// </summary>
public static class HierarchyRegistry
{
    private static readonly bool[]?[] _parentToChildLookup 
        = new bool[]?[Constants.MaxEntities];

    /// <summary>
    /// Tracks a child entity to the specified parent.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The child entity to add.</param>
    public static void TrackChild(Entity parent, Entity child)
    {
        _parentToChildLookup[parent.Index] ??= new bool[Constants.MaxEntities];
        _parentToChildLookup[parent.Index]![child.Index] = true;
    }

    /// <summary>
    /// Removes tracking of a child entity from the specified parent.
    /// Throws an exception if the parent never had children tracked.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The child entity to remove.</param>
    public static void UntrackChild(Entity parent, Entity child)
    {
        if (_parentToChildLookup[parent.Index] is null) 
        {
            throw new InvalidOperationException(
                $"Attempted to remove a child from a parent({parent.Index}) that was never tracked."
            );
        }

        _parentToChildLookup[parent.Index]![child.Index] = false;
    }

    /// <summary>
    /// Retrieves all child entities of the specified parent.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <returns>An enumerable of child entities belonging to the parent.</returns>
    public static IEnumerable<Entity> GetChildren(Entity parent)
    {
        var children = _parentToChildLookup[parent.Index];
        if (children == null)
        {
            yield break;
        }

        for (ushort child = 0; child < Constants.MaxEntities; child++)
        {
            if (children[child])
            {
                yield return new Entity(child);
            }
        }
    }

    /// <summary>
    /// Checks whether the specified child entity belongs to the given parent.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The child entity to check for.</param>
    /// <returns>True if the child belongs to the parent; otherwise, false.</returns>
    public static bool HasChild(Entity parent, Entity child)
    {
        return _parentToChildLookup[parent.Index]?[child.Index] ?? false;
    }

    public static bool HasAnyChildren(Entity parent)
    {
        return _parentToChildLookup[parent.Index]?.Any(static c => c) ?? false;
    }
}


