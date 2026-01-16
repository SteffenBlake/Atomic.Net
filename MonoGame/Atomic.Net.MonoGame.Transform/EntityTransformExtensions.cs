using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Extension methods for Entity that provide transform-related functionality.
/// </summary>
public static class EntityExtensions
{
    /// <summary>
    /// Sets up transform behavior for an entity and ensures WorldTransform is initialized.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="mutate">Action to mutate the transform behavior's backing values.</param>
    /// <returns>The entity for fluent chaining.</returns>
    public static Entity WithTransform(
        this Entity entity, RefReadonlyAction<TransformBehavior> mutate
    )
    {
        // Set the TransformBehavior
        entity.SetRefBehavior(mutate);
        
        // Ensure WorldTransform behavior is initialized
        entity.SetRefBehavior((ref readonly WorldTransformBehavior _) => { });
       
        ParentWorldTransformBackingStore.Instance.EnsureFor(entity);

        return entity;
    }

    /// <summary>
    /// Sets up transform behavior for an entity with default values and ensures WorldTransform is initialized.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The entity for fluent chaining.</returns>
    public static Entity WithTransform(this Entity entity)
    {
        return entity.WithTransform((ref readonly _) => { });
    }
}
