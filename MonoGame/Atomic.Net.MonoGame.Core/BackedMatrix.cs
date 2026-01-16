namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// A 4x4 Matrix backed by sixteen InputBlockMap properties.
/// </summary>
public readonly struct BackedMatrix(
    BackedFloat m11, BackedFloat m12, BackedFloat m13, BackedFloat m14,
    BackedFloat m21, BackedFloat m22, BackedFloat m23, BackedFloat m24,
    BackedFloat m31, BackedFloat m32, BackedFloat m33, BackedFloat m34,
    BackedFloat m41, BackedFloat m42, BackedFloat m43, BackedFloat m44
)
{
    public readonly BackedFloat M11 = m11;
    public readonly BackedFloat M12 = m12;
    public readonly BackedFloat M13 = m13;
    public readonly BackedFloat M14 = m14;
    public readonly BackedFloat M21 = m21;
    public readonly BackedFloat M22 = m22;
    public readonly BackedFloat M23 = m23;
    public readonly BackedFloat M24 = m24;
    public readonly BackedFloat M31 = m31;
    public readonly BackedFloat M32 = m32;
    public readonly BackedFloat M33 = m33;
    public readonly BackedFloat M34 = m34;
    public readonly BackedFloat M41 = m41;
    public readonly BackedFloat M42 = m42;
    public readonly BackedFloat M43 = m43;
    public readonly BackedFloat M44 = m44;

    public Microsoft.Xna.Framework.Matrix AsMatrix() => new(
        M11.Value, M12.Value, M13.Value, M14.Value,
        M21.Value, M22.Value, M23.Value, M24.Value,
        M31.Value, M32.Value, M33.Value, M34.Value,
        M41.Value, M42.Value, M43.Value, M44.Value
    );
}
