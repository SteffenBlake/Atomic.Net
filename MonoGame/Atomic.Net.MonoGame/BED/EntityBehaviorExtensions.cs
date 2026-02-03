using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED;

/// <summary>
/// Extension methods for Entity that provide behavior-related functionality.
/// </summary>
public static class EntityBehaviorExtensions
{
    /// <summary>
    /// Sets or replaces a regular (non-backed) behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="mutate">Action to mutate the behavior by reference.</param>
    /// <returns>The entity for fluent chaining.</returns>
    public static Entity SetBehavior<TBehavior>(this Entity entity, RefAction<TBehavior> mutate)
        where TBehavior : struct
    {
        BehaviorRegistry<TBehavior>.Instance.SetBehavior(entity, mutate);
        return entity;
    }

    /// <summary>
    /// Sets or replaces a regular (non-backed) behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="mutate">Action to mutate the behavior by reference.</param>
    /// <returns>The entity for fluent chaining.</returns>
    public static Entity SetBehavior<TBehavior, THelper>(
        this Entity entity, ref readonly THelper helper, RefInAction<TBehavior, THelper> mutate
    )
        where TBehavior : struct
    {
        BehaviorRegistry<TBehavior>.Instance.SetBehavior(entity, in helper, mutate);
        return entity;
    }


    /// <summary>
    /// Removes a behavior from this entity.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>True if the behavior was removed.</returns>
    public static bool RemoveBehavior<TBehavior>(this Entity entity)
        where TBehavior : struct
    {
        return BehaviorRegistry<TBehavior>.Instance.Remove(entity);
    }

    /// <summary>
    /// Attempts to retrieve a behavior from this entity.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="behavior">The behavior instance.</param>
    /// <returns>True if the behavior exists.</returns>
    public static bool TryGetBehavior<TBehavior>(
        this Entity entity,
        [NotNullWhen(true)] out TBehavior? behavior
    )
        where TBehavior : struct
    {
        return BehaviorRegistry<TBehavior>.Instance.TryGetBehavior(entity, out behavior);
    }

    /// <summary>
    /// Checks whether this entity has a specific behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>True if the behavior is active.</returns>
    public static bool HasBehavior<TBehavior>(this Entity entity)
        where TBehavior : struct
    {
        return BehaviorRegistry<TBehavior>.Instance.HasBehavior(entity);
    }
}
