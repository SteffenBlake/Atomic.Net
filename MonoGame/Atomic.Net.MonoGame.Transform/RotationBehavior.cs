using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the local rotation input for an entity.
/// </summary>
public readonly record struct RotationBehavior(
    BackedProperty<float> X,
    BackedProperty<float> Y,
    BackedProperty<float> Z,
    BackedProperty<float> W
)
{
    public static RotationBehavior Create(int entityIndex) => new(
        RotationBackingStore.Instance.X.InstanceFor(entityIndex),
        RotationBackingStore.Instance.Y.InstanceFor(entityIndex),
        RotationBackingStore.Instance.Z.InstanceFor(entityIndex),
        RotationBackingStore.Instance.W.InstanceFor(entityIndex)
    );

    /// <summary>
    /// Gets a BackedQuaternion view of this rotation.
    /// </summary>
    public BackedQuaternion AsQuaternion() => new(X, Y, Z, W);
}

