using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the local position input for an entity.
/// </summary>
public readonly record struct PositionBehavior(
    BackedProperty<float> X,
    BackedProperty<float> Y,
    BackedProperty<float> Z
)
{
    public static PositionBehavior Create(int entityIndex) => new(
        PositionBackingStore.Instance.X.InstanceFor(entityIndex),
        PositionBackingStore.Instance.Y.InstanceFor(entityIndex),
        PositionBackingStore.Instance.Z.InstanceFor(entityIndex)
    );

    /// <summary>
    /// Gets a BackedVector3 view of this position.
    /// </summary>
    public BackedVector3 AsVector3() => new(X, Y, Z);
}

