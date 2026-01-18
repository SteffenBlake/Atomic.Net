namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// A property backed by an array.
/// </summary>
public readonly struct BackedFloat(float[] backing, ushort entityIndex)
{
    public float Value
    {
        get => backing[entityIndex];
        set => backing[entityIndex] = value;
    }
}
