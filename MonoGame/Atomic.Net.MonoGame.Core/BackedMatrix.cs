using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// A Matrix backed by 16 BackedProperty<float> components.
/// Provides conversion to/from Matrix (4x4 matrix).
/// </summary>
public readonly struct BackedMatrix(
    BackedProperty<float> m11, BackedProperty<float> m12, BackedProperty<float> m13, BackedProperty<float> m14,
    BackedProperty<float> m21, BackedProperty<float> m22, BackedProperty<float> m23, BackedProperty<float> m24,
    BackedProperty<float> m31, BackedProperty<float> m32, BackedProperty<float> m33, BackedProperty<float> m34,
    BackedProperty<float> m41, BackedProperty<float> m42, BackedProperty<float> m43, BackedProperty<float> m44)
{
    public readonly BackedProperty<float> M11 = m11;
    public readonly BackedProperty<float> M12 = m12;
    public readonly BackedProperty<float> M13 = m13;
    public readonly BackedProperty<float> M14 = m14;
    public readonly BackedProperty<float> M21 = m21;
    public readonly BackedProperty<float> M22 = m22;
    public readonly BackedProperty<float> M23 = m23;
    public readonly BackedProperty<float> M24 = m24;
    public readonly BackedProperty<float> M31 = m31;
    public readonly BackedProperty<float> M32 = m32;
    public readonly BackedProperty<float> M33 = m33;
    public readonly BackedProperty<float> M34 = m34;
    public readonly BackedProperty<float> M41 = m41;
    public readonly BackedProperty<float> M42 = m42;
    public readonly BackedProperty<float> M43 = m43;
    public readonly BackedProperty<float> M44 = m44;

    /// <summary>
    /// Converts the backed components to a Matrix.
    /// </summary>
    public Matrix AsMatrix() => new(
        M11.Value, M12.Value, M13.Value, M14.Value,
        M21.Value, M22.Value, M23.Value, M24.Value,
        M31.Value, M32.Value, M33.Value, M34.Value,
        M41.Value, M42.Value, M43.Value, M44.Value
    );
}
