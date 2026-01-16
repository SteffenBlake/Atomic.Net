using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the anchor/pivot point for an entity.
/// </summary>
public readonly record struct AnchorBehavior : IBehavior<AnchorBehavior>
{
    public readonly BackedProperty<float> X;
    public readonly BackedProperty<float> Y;
    public readonly BackedProperty<float> Z;

    public AnchorBehavior(BackedProperty<float> x, BackedProperty<float> y, BackedProperty<float> z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static AnchorBehavior CreateFor(Entity entity)
    {
        return AnchorBackingStore.Instance.CreateFor(entity);
    }
}

