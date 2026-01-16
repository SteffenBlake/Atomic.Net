using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// A Quaternion backed by four BackedProperty<float> components.
/// Provides conversion to/from Quaternion.
/// </summary>
public readonly struct BackedQuaternion(
    BackedProperty<float> x,
    BackedProperty<float> y,
    BackedProperty<float> z,
    BackedProperty<float> w)
{
    public readonly BackedProperty<float> X = x;
    public readonly BackedProperty<float> Y = y;
    public readonly BackedProperty<float> Z = z;
    public readonly BackedProperty<float> W = w;

    /// <summary>
    /// Converts the backed components to a Quaternion.
    /// </summary>
    public Quaternion AsQuaternion() => new(X.Value, Y.Value, Z.Value, W.Value);
}
