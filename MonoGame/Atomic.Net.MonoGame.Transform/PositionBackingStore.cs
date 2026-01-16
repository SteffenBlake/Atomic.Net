using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.BlockMaps;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Backing store for position behavior data.
/// </summary>
public sealed class PositionBackingStore : ISingleton<PositionBackingStore>
{
    public static PositionBackingStore Instance { get; } = new();

    public InputBlockMap X { get; } = new();
    public InputBlockMap Y { get; } = new();
    public InputBlockMap Z { get; } = new();

    /// <summary>
    /// Creates a BackedVector3 for the specified entity index.
    /// </summary>
    public BackedVector3 Build(int entityIndex) => new(
        X.InstanceFor(entityIndex),
        Y.InstanceFor(entityIndex),
        Z.InstanceFor(entityIndex)
    );
}
