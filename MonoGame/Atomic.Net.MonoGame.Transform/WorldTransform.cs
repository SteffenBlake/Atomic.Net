using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the final world transform of an entity, calculated from inputs and parent hierarchy.
/// </summary>
public readonly record struct WorldTransform : IBehavior<WorldTransform>
{
    public readonly BackedProperty<float> M11;
    public readonly BackedProperty<float> M12;
    public readonly BackedProperty<float> M13;
    public readonly BackedProperty<float> M14;
    public readonly BackedProperty<float> M21;
    public readonly BackedProperty<float> M22;
    public readonly BackedProperty<float> M23;
    public readonly BackedProperty<float> M24;
    public readonly BackedProperty<float> M31;
    public readonly BackedProperty<float> M32;
    public readonly BackedProperty<float> M33;
    public readonly BackedProperty<float> M34;
    public readonly BackedProperty<float> M41;
    public readonly BackedProperty<float> M42;
    public readonly BackedProperty<float> M43;
    public readonly BackedProperty<float> M44;

    public WorldTransform(
        BackedProperty<float> m11, BackedProperty<float> m12, BackedProperty<float> m13, BackedProperty<float> m14,
        BackedProperty<float> m21, BackedProperty<float> m22, BackedProperty<float> m23, BackedProperty<float> m24,
        BackedProperty<float> m31, BackedProperty<float> m32, BackedProperty<float> m33, BackedProperty<float> m34,
        BackedProperty<float> m41, BackedProperty<float> m42, BackedProperty<float> m43, BackedProperty<float> m44)
    {
        M11 = m11; M12 = m12; M13 = m13; M14 = m14;
        M21 = m21; M22 = m22; M23 = m23; M24 = m24;
        M31 = m31; M32 = m32; M33 = m33; M34 = m34;
        M41 = m41; M42 = m42; M43 = m43; M44 = m44;
    }

    public static WorldTransform CreateFor(int entityIndex)
    {
        return WorldTransformBackingStore.Instance.CreateFor(entityIndex);
    }
}


