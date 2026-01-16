namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// A property backed by an external array. Reads and writes go directly to the backing store.
/// </summary>
public readonly struct BackedProperty<T>(T[] backing, int index)
    where T : struct
{
    public T Value
    {
        get => backing[index];
        set => backing[index] = value;
    }

    public static implicit operator T(BackedProperty<T> prop) => prop.Value;
}
