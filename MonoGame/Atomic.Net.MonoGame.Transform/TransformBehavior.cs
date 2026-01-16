using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores all transform inputs: position, rotation, scale, anchor.
/// </summary>
public readonly record struct TransformBehavior(
    BackedVector3 Position,
    BackedQuaternion Rotation,
    BackedVector3 Scale,
    BackedVector3 Anchor
) : IBehavior<TransformBehavior>
{
    public static TransformBehavior CreateFor(Entity entity)
    {
        return TransformBackingStore.Instance.CreateFor(entity);
    }
}
