using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// A 4x4 Matrix backed by sixteen InputBlockMap properties.
/// </summary>
public readonly struct ReadOnlyBackedMatrix(
    ReadOnlyBackedFloat m11,
    ReadOnlyBackedFloat m12,
    ReadOnlyBackedFloat m13,
    ReadOnlyBackedFloat m14,
    ReadOnlyBackedFloat m21,
    ReadOnlyBackedFloat m22,
    ReadOnlyBackedFloat m23,
    ReadOnlyBackedFloat m24,
    ReadOnlyBackedFloat m31,
    ReadOnlyBackedFloat m32,
    ReadOnlyBackedFloat m33,
    ReadOnlyBackedFloat m34,
    ReadOnlyBackedFloat m41,
    ReadOnlyBackedFloat m42,
    ReadOnlyBackedFloat m43,
    ReadOnlyBackedFloat m44
)
{
    public readonly ReadOnlyBackedFloat M11 = m11;
    public readonly ReadOnlyBackedFloat M12 = m12;
    public readonly ReadOnlyBackedFloat M13 = m13;
    public readonly ReadOnlyBackedFloat M14 = m14;
    public readonly ReadOnlyBackedFloat M21 = m21;
    public readonly ReadOnlyBackedFloat M22 = m22;
    public readonly ReadOnlyBackedFloat M23 = m23;
    public readonly ReadOnlyBackedFloat M24 = m24;
    public readonly ReadOnlyBackedFloat M31 = m31;
    public readonly ReadOnlyBackedFloat M32 = m32;
    public readonly ReadOnlyBackedFloat M33 = m33;
    public readonly ReadOnlyBackedFloat M34 = m34;
    public readonly ReadOnlyBackedFloat M41 = m41;
    public readonly ReadOnlyBackedFloat M42 = m42;
    public readonly ReadOnlyBackedFloat M43 = m43;
    public readonly ReadOnlyBackedFloat M44 = m44;

    public Matrix AsMatrix() => new(
        M11.Value, M12.Value, M13.Value, M14.Value,
        M21.Value, M22.Value, M23.Value, M24.Value,
        M31.Value, M32.Value, M33.Value, M34.Value,
        M41.Value, M42.Value, M43.Value, M44.Value
    );
}

