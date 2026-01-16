using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.BlockMaps;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Backing store for scale behavior data.
/// </summary>
public sealed class ScaleBackingStore : ISingleton<ScaleBackingStore>
{
    public static ScaleBackingStore Instance { get; } = new();

    public InputBlockMap X { get; } = new();
    public InputBlockMap Y { get; } = new();
    public InputBlockMap Z { get; } = new();

    /// <summary>
    /// Creates a ScaleBehavior for the specified entity.
    /// </summary>
    public ScaleBehavior CreateFor(Entity entity) => new(
        X.InstanceFor(entity.Index),
        Y.InstanceFor(entity.Index),
        Z.InstanceFor(entity.Index)
    );
}
