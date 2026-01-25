namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Event raised when an entity is Disabled.
/// </summary>
/// <param name="entity">The disabled entity.</param>
public readonly struct EntityDisabledEvent(Entity entity)
{
    /// <summary>
    /// The disabled entity.
    /// </summary>
    public readonly Entity Entity = entity;
}


