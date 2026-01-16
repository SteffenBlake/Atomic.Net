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
    /// Creates a PositionBehavior for the specified entity.
    /// </summary>
    public PositionBehavior CreateFor(Entity entity) => new(
        X.InstanceFor(entity.Index),
        Y.InstanceFor(entity.Index),
        Z.InstanceFor(entity.Index)
    );
}
