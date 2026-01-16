using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the final world transform of an entity, calculated from inputs and parent hierarchy.
/// </summary>
public readonly record struct WorldTransform(BackedMatrix Value) : IBehavior<WorldTransform>
{
    public static WorldTransform CreateFor(Entity entity)
    {
        return WorldTransformBackingStore.Instance.CreateFor(entity);
    }
}


