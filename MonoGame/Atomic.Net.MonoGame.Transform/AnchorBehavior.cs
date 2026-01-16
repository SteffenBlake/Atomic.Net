using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the anchor/pivot point for an entity.
/// </summary>
public readonly record struct AnchorBehavior(BackedVector3 Value) : IBehavior<AnchorBehavior>
{
    public static AnchorBehavior CreateFor(Entity entity)
    {
        return AnchorBackingStore.Instance.CreateFor(entity);
    }
}

