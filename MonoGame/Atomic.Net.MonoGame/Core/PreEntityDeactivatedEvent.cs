namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Event raised when an entity is about to be deactivated.
/// </summary>
/// <param name="entity">The deactivated entity.</param>
public readonly struct PreEntityDeactivatedEvent(Entity entity)
{
    /// <summary>
    /// The deactivated entity.
    /// </summary>
    public readonly Entity Entity = entity;
}

