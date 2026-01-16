using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the final world transform of an entity, calculated from inputs and parent hierarchy.
/// </summary>
public readonly record struct WorldTransform(
    BackedProperty<float> M11, BackedProperty<float> M12, BackedProperty<float> M13, BackedProperty<float> M14,
    BackedProperty<float> M21, BackedProperty<float> M22, BackedProperty<float> M23, BackedProperty<float> M24,
    BackedProperty<float> M31, BackedProperty<float> M32, BackedProperty<float> M33, BackedProperty<float> M34,
    BackedProperty<float> M41, BackedProperty<float> M42, BackedProperty<float> M43, BackedProperty<float> M44
)
{
    public static WorldTransform Create(int entityIndex) => new(
        WorldTransformBackingStore.Instance.M11.InstanceFor(entityIndex),
        WorldTransformBackingStore.Instance.M12.InstanceFor(entityIndex),
        WorldTransformBackingStore.Instance.M13.InstanceFor(entityIndex),
        WorldTransformBackingStore.Instance.M14.InstanceFor(entityIndex),
        WorldTransformBackingStore.Instance.M21.InstanceFor(entityIndex),
        WorldTransformBackingStore.Instance.M22.InstanceFor(entityIndex),
        WorldTransformBackingStore.Instance.M23.InstanceFor(entityIndex),
        WorldTransformBackingStore.Instance.M24.InstanceFor(entityIndex),
        WorldTransformBackingStore.Instance.M31.InstanceFor(entityIndex),
        WorldTransformBackingStore.Instance.M32.InstanceFor(entityIndex),
        WorldTransformBackingStore.Instance.M33.InstanceFor(entityIndex),
        WorldTransformBackingStore.Instance.M34.InstanceFor(entityIndex),
        WorldTransformBackingStore.Instance.M41.InstanceFor(entityIndex),
        WorldTransformBackingStore.Instance.M42.InstanceFor(entityIndex),
        WorldTransformBackingStore.Instance.M43.InstanceFor(entityIndex),
        WorldTransformBackingStore.Instance.M44.InstanceFor(entityIndex)
    );

    /// <summary>
    /// Gets a BackedMatrix view of this transform.
    /// </summary>
    public BackedMatrix AsMatrix() => new(
        M11, M12, M13, M14,
        M21, M22, M23, M24,
        M31, M32, M33, M34,
        M41, M42, M43, M44
    );
}


