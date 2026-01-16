using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// A Quaternion backed by four BackedProperty<float> components.
/// Provides conversion to/from Quaternion.
/// </summary>
public readonly struct BackedQuaternion
{
    private readonly BackedProperty<float> _x;
    private readonly BackedProperty<float> _y;
    private readonly BackedProperty<float> _z;
    private readonly BackedProperty<float> _w;

    public BackedQuaternion(
        BackedProperty<float> x,
        BackedProperty<float> y,
        BackedProperty<float> z,
        BackedProperty<float> w)
    {
        _x = x;
        _y = y;
        _z = z;
        _w = w;
    }

    public BackedProperty<float> X => _x;
    public BackedProperty<float> Y => _y;
    public BackedProperty<float> Z => _z;
    public BackedProperty<float> W => _w;

    /// <summary>
    /// Converts the backed components to a Quaternion.
    /// </summary>
    public Quaternion ToQuaternion() => new(X.Value, Y.Value, Z.Value, W.Value);

    /// <summary>
    /// Sets the backed components from a Quaternion.
    /// </summary>
    public void FromQuaternion(Quaternion value)
    {
        _x.Value = value.X;
        _y.Value = value.Y;
        _z.Value = value.Z;
        _w.Value = value.W;
    }

    public static implicit operator Quaternion(BackedQuaternion backed) => backed.ToQuaternion();
}
