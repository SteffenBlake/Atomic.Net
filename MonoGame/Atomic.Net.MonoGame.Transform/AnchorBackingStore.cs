using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.BlockMaps;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Backing store for anchor behavior data.
/// </summary>
public sealed class AnchorBackingStore : ISingleton<AnchorBackingStore>
{
    public static AnchorBackingStore Instance { get; } = new();

    public InputBlockMap X { get; } = new();
    public InputBlockMap Y { get; } = new();
    public InputBlockMap Z { get; } = new();

    /// <summary>
    /// Creates an AnchorBehavior for the specified entity.
    /// </summary>
    public AnchorBehavior CreateFor(Entity entity) => new(
        new BackedVector3(
            X.InstanceFor(entity.Index),
            Y.InstanceFor(entity.Index),
            Z.InstanceFor(entity.Index)
        )
    );
}
