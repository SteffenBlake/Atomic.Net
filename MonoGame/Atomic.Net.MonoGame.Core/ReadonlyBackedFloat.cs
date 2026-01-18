using Atomic.Net.MonoGame.Core.BlockMaps;

namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// A float property backed by an InputBlockMap. Writes trigger dirty propagation.
/// </summary>
public readonly struct ReadOnlyBackedFloat(BlockMapBase backing, int entityIndex)
{
    public float Value
    {
        get => backing[entityIndex] ?? 0f;
    }
}


