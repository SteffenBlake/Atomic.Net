namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// A Quaternion backed by four InputBlockMap properties.
/// </summary>
public readonly struct BackedQuaternion(
    BackedFloat x,
    BackedFloat y,
    BackedFloat z,
    BackedFloat w
)
{
    public readonly BackedFloat X = x;
    public readonly BackedFloat Y = y;
    public readonly BackedFloat Z = z;
    public readonly BackedFloat W = w;

    public Microsoft.Xna.Framework.Quaternion AsQuaternion() => new(X.Value, Y.Value, Z.Value, W.Value);
}
