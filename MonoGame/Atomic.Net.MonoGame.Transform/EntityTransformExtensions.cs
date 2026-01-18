using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Extension methods for Entity that provide transform-related functionality.
/// </summary>
public static class EntityExtensions
{
    /// <summary>
    /// Sets up transform behavior for an entity. The TransformRegistry will automatically initialize
    /// all related backing stores and the WorldTransformBehavior.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="mutate">Action to mutate the transform behavior.</param>
    /// <returns>The entity for fluent chaining.</returns>
    public static Entity WithTransform(
        this Entity entity, RefAction<TransformBehavior> mutate
    )
    {
        BehaviorRegistry<TransformBehavior>.Instance.SetBehavior(entity, mutate);
        return entity;
    }

    /// <summary>
    /// Sets up transform behavior for an entity with default values. The TransformRegistry will
    /// automatically initialize all related backing stores and the WorldTransformBehavior.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The entity for fluent chaining.</returns>
    public static Entity WithTransform(this Entity entity)
    {
        BehaviorRegistry<TransformBehavior>.Instance.SetBehavior(entity, (ref TransformBehavior t) =>
        {
            t.Position = Vector3.Zero;
            t.Rotation = Quaternion.Identity;
            t.Scale = Vector3.One;
            t.Anchor = Vector3.Zero;
        });
        return entity;
    }
}
