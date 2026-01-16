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
    public readonly BackedProperty<float> M11 = m11, M12 = m12, M13 = m13, M14 = m14;
    public readonly BackedProperty<float> M21 = m21, M22 = m22, M23 = m23, M24 = m24;
    public readonly BackedProperty<float> M31 = m31, M32 = m32, M33 = m33, M34 = m34;
    public readonly BackedProperty<float> M41 = m41, M42 = m42, M43 = m43, M44 = m44;

    /// <summary>
    /// Converts the backed components to a Matrix.
    /// </summary>
    public Matrix ToMatrix() => new(
        M11.Value, M12.Value, M13.Value, M14.Value,
        M21.Value, M22.Value, M23.Value, M24.Value,
        M31.Value, M32.Value, M33.Value, M34.Value,
        M41.Value, M42.Value, M43.Value, M44.Value
    );

    /// <summary>
    /// Sets the backed components from a Matrix.
    /// </summary>
    public void FromMatrix(Matrix value)
    {
        M11.Value = value.M11; M12.Value = value.M12; M13.Value = value.M13; M14.Value = value.M14;
        M21.Value = value.M21; M22.Value = value.M22; M23.Value = value.M23; M24.Value = value.M24;
        M31.Value = value.M31; M32.Value = value.M32; M33.Value = value.M33; M34.Value = value.M34;
        M41.Value = value.M41; M42.Value = value.M42; M43.Value = value.M43; M44.Value = value.M44;
    }

    public static implicit operator Matrix(BackedMatrix backed) => backed.ToMatrix();
}
