using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the local position input for an entity.
/// </summary>
public readonly record struct PositionBehavior : IBehavior<PositionBehavior>
{
    public readonly BackedProperty<float> X;
    public readonly BackedProperty<float> Y;
    public readonly BackedProperty<float> Z;

    public PositionBehavior(BackedProperty<float> x, BackedProperty<float> y, BackedProperty<float> z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static PositionBehavior CreateFor(Entity entity)
    {
        return PositionBackingStore.Instance.CreateFor(entity);
    }
}

