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
    public readonly BackedProperty<float> X = x;
    public readonly BackedProperty<float> Y = y;
    public readonly BackedProperty<float> Z = z;

    /// <summary>
    /// Converts the backed components to a Vector3.
    /// </summary>
    public Vector3 AsVector3() => new(X.Value, Y.Value, Z.Value);

    /// <summary>
    /// Sets the backed components from a Vector3.
    /// </summary>
    public void FromVector3(Vector3 value)
    {
        X.Value = value.X;
        Y.Value = value.Y;
        Z.Value = value.Z;
    }

    public static implicit operator Vector3(BackedVector3 backed) => backed.AsVector3();
}
