using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the local rotation input for an entity.
/// </summary>
public readonly record struct RotationBehavior(BackedQuaternion Value) : IBehavior<RotationBehavior>
{
    public static RotationBehavior CreateFor(Entity entity)
    {
        return RotationBackingStore.Instance.CreateFor(entity);
    }
}

