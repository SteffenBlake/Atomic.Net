using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the local scale input for an entity.
/// </summary>
public readonly record struct ScaleBehavior : IBehavior<ScaleBehavior>
{
    public readonly BackedProperty<float> X;
    public readonly BackedProperty<float> Y;
    public readonly BackedProperty<float> Z;

    public ScaleBehavior(BackedProperty<float> x, BackedProperty<float> y, BackedProperty<float> z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static ScaleBehavior CreateFor(int entityIndex)
    {
        return ScaleBackingStore.Instance.CreateFor(entityIndex);
    }
}

