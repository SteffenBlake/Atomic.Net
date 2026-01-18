namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// A Quaternion backed by four InputBlockMap properties.
/// </summary>
public readonly struct ReadOnlyBackedQuaternion(
    ReadOnlyBackedFloat x,
    ReadOnlyBackedFloat y,
    ReadOnlyBackedFloat z,
    ReadOnlyBackedFloat w
)
{
    public readonly ReadOnlyBackedFloat X = x;
    public readonly ReadOnlyBackedFloat Y = y;
    public readonly ReadOnlyBackedFloat Z = z;
    public readonly ReadOnlyBackedFloat W = w;

    public Microsoft.Xna.Framework.Quaternion AsQuaternion() => 
        new(X.Value, Y.Value, Z.Value, W.Value);
}

