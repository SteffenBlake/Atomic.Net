using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the anchor/pivot point for an entity.
/// </summary>
public readonly record struct AnchorBehavior(
    BackedProperty<float> X,
    BackedProperty<float> Y,
    BackedProperty<float> Z
)
{
    public static AnchorBehavior Create(int entityIndex) => new(
        AnchorBackingStore.Instance.X.InstanceFor(entityIndex),
        AnchorBackingStore.Instance.Y.InstanceFor(entityIndex),
        AnchorBackingStore.Instance.Z.InstanceFor(entityIndex)
    );

    /// <summary>
    /// Gets a BackedVector3 view of this anchor.
    /// </summary>
    public BackedVector3 AsVector3() => new(X, Y, Z);
}

