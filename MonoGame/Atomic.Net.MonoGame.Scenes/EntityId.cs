using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Behavior that tracks an entity's string ID for referencing in JSON.
/// Enables parent references like "parent": "#player-entity".
/// </summary>
public readonly record struct EntityId(string Id) : IBehavior<EntityId>
{
    public static EntityId CreateFor(Entity entity)
    {
        return new EntityId(string.Empty);
    }
}
