using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the local rotation input for an entity.
/// </summary>
public readonly record struct RotationBehavior : IBehavior<RotationBehavior>
{
    public readonly BackedProperty<float> X;
    public readonly BackedProperty<float> Y;
    public readonly BackedProperty<float> Z;
    public readonly BackedProperty<float> W;

    public RotationBehavior(BackedProperty<float> x, BackedProperty<float> y, BackedProperty<float> z, BackedProperty<float> w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public static RotationBehavior CreateFor(int entityIndex)
    {
        return RotationBackingStore.Instance.CreateFor(entityIndex);
    }
}

