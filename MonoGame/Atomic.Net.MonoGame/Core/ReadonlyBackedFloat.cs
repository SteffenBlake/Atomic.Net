namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// A float property backed by an array.
/// </summary>
public readonly struct ReadOnlyBackedFloat(float[] backing, ushort entityIndex)
{
    public float Value
    {
        get => backing[entityIndex];
    }
}
