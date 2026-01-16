using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the local scale input for an entity.
/// </summary>
public readonly record struct ScaleBehavior(
    BackedProperty<float> X,
    BackedProperty<float> Y,
    BackedProperty<float> Z
)
{
    public static ScaleBehavior Create(int entityIndex) => new(
        ScaleBackingStore.Instance.X.InstanceFor(entityIndex),
        ScaleBackingStore.Instance.Y.InstanceFor(entityIndex),
        ScaleBackingStore.Instance.Z.InstanceFor(entityIndex)
    );

    /// <summary>
    /// Gets a BackedVector3 view of this scale.
    /// </summary>
    public BackedVector3 AsVector3() => new(X, Y, Z);
}

