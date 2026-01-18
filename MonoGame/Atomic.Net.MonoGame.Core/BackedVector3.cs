using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// A Vector3 backed by three InputBlockMap properties.
/// </summary>
public readonly struct BackedVector3(BackedFloat x, BackedFloat y, BackedFloat z)
{
    public readonly BackedFloat X = x;
    public readonly BackedFloat Y = y;
    public readonly BackedFloat Z = z;

    public Vector3 AsVector3() => new(X.Value, Y.Value, Z.Value);
}
