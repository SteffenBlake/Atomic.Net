using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED;

/// <summary>
/// Behavior that tracks an entity's string ID for referencing in JSON.
/// Enables parent references like "parent": "#player-entity".
/// </summary>
public readonly record struct IdBehavior(string Id) : IBehavior<IdBehavior>
{
    public static IdBehavior CreateFor(Entity entity)
    {
        return new IdBehavior(string.Empty);
    }
}
