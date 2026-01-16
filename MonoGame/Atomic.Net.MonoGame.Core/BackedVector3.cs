using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// A Vector3 backed by three BackedProperty<float> components.
/// Provides conversion to/from Vector3.
/// </summary>
public readonly struct BackedVector3(
    BackedProperty<float> x,
    BackedProperty<float> y,
    BackedProperty<float> z)
{
    private readonly BackedProperty<float> _x = x;
    private readonly BackedProperty<float> _y = y;
    private readonly BackedProperty<float> _z = z;

    public BackedProperty<float> X => _x;
    public BackedProperty<float> Y => _y;
    public BackedProperty<float> Z => _z;

    /// <summary>
    /// Converts the backed components to a Vector3.
    /// </summary>
    public Vector3 ToVector3() => new(X.Value, Y.Value, Z.Value);

    /// <summary>
    /// Sets the backed components from a Vector3.
    /// </summary>
    public void FromVector3(Vector3 value)
    {
        _x.Value = value.X;
        _y.Value = value.Y;
        _z.Value = value.Z;
    }

    public static implicit operator Vector3(BackedVector3 backed) => backed.ToVector3();
}
