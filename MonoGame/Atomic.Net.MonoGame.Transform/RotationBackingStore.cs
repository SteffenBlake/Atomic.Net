using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.BlockMaps;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Backing store for rotation behavior data.
/// </summary>
public sealed class RotationBackingStore : ISingleton<RotationBackingStore>
{
    public static RotationBackingStore Instance { get; } = new();

    public InputBlockMap X { get; } = new();
    public InputBlockMap Y { get; } = new();
    public InputBlockMap Z { get; } = new();
    public InputBlockMap W { get; } = new();
}
