namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// A Vector3 backed by three InputBlockMap properties.
/// </summary>
public readonly struct ReadOnlyBackedVector3(
    ReadOnlyBackedFloat x, ReadOnlyBackedFloat y, ReadOnlyBackedFloat z
)
{
    public readonly ReadOnlyBackedFloat X = x;
    public readonly ReadOnlyBackedFloat Y = y;
    public readonly ReadOnlyBackedFloat Z = z;

    public Microsoft.Xna.Framework.Vector3 AsVector3() => new(X.Value, Y.Value, Z.Value);
}

