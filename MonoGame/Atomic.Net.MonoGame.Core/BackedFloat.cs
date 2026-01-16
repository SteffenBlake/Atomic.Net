using Atomic.Net.MonoGame.Core.BlockMaps;

namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// A float property backed by an InputBlockMap. Writes trigger dirty propagation.
/// </summary>
public readonly struct BackedFloat(InputBlockMap backing, int entityIndex)
{
    private readonly InputBlockMap _backing = backing;
    private readonly int _entityIndex = entityIndex;

    public float Value
    {
        get => _backing[_entityIndex] ?? 0f;
        set => _backing.Set(_entityIndex, value);
    }

    public static implicit operator float(BackedFloat prop) => prop.Value;
}
