using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the final world transform of an entity, calculated from inputs and parent hierarchy.
/// </summary>
public readonly record struct WorldTransformBehavior(ReadOnlyBackedMatrix Value) 
    : IBehavior<WorldTransformBehavior>
{
    public static WorldTransformBehavior CreateFor(Entity entity)
    {
        return WorldTransformBackingStore.CreateFor(entity);
    }
}

