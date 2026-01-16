using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// A Matrix backed by 16 BackedProperty<float> components.
/// Provides conversion to/from Matrix (4x4 matrix).
/// </summary>
public readonly struct BackedMatrix
{
    private readonly BackedProperty<float> _m11, _m12, _m13, _m14;
    private readonly BackedProperty<float> _m21, _m22, _m23, _m24;
    private readonly BackedProperty<float> _m31, _m32, _m33, _m34;
    private readonly BackedProperty<float> _m41, _m42, _m43, _m44;

    public BackedMatrix(
        BackedProperty<float> m11, BackedProperty<float> m12, BackedProperty<float> m13, BackedProperty<float> m14,
        BackedProperty<float> m21, BackedProperty<float> m22, BackedProperty<float> m23, BackedProperty<float> m24,
        BackedProperty<float> m31, BackedProperty<float> m32, BackedProperty<float> m33, BackedProperty<float> m34,
        BackedProperty<float> m41, BackedProperty<float> m42, BackedProperty<float> m43, BackedProperty<float> m44)
    {
        _m11 = m11; _m12 = m12; _m13 = m13; _m14 = m14;
        _m21 = m21; _m22 = m22; _m23 = m23; _m24 = m24;
        _m31 = m31; _m32 = m32; _m33 = m33; _m34 = m34;
        _m41 = m41; _m42 = m42; _m43 = m43; _m44 = m44;
    }

    public BackedProperty<float> M11 => _m11;
    public BackedProperty<float> M12 => _m12;
    public BackedProperty<float> M13 => _m13;
    public BackedProperty<float> M14 => _m14;
    
    public BackedProperty<float> M21 => _m21;
    public BackedProperty<float> M22 => _m22;
    public BackedProperty<float> M23 => _m23;
    public BackedProperty<float> M24 => _m24;
    
    public BackedProperty<float> M31 => _m31;
    public BackedProperty<float> M32 => _m32;
    public BackedProperty<float> M33 => _m33;
    public BackedProperty<float> M34 => _m34;
    
    public BackedProperty<float> M41 => _m41;
    public BackedProperty<float> M42 => _m42;
    public BackedProperty<float> M43 => _m43;
    public BackedProperty<float> M44 => _m44;

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
        _m11.Value = value.M11; _m12.Value = value.M12; _m13.Value = value.M13; _m14.Value = value.M14;
        _m21.Value = value.M21; _m22.Value = value.M22; _m23.Value = value.M23; _m24.Value = value.M24;
        _m31.Value = value.M31; _m32.Value = value.M32; _m33.Value = value.M33; _m34.Value = value.M34;
        _m41.Value = value.M41; _m42.Value = value.M42; _m43.Value = value.M43; _m44.Value = value.M44;
    }

    public static implicit operator Matrix(BackedMatrix backed) => backed.ToMatrix();
}
