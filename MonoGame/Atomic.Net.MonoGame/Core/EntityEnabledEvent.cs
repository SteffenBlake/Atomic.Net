namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Event raised when an entity is Enabled.
/// </summary>
/// <param name="entity">The enabled entity.</param>
public readonly struct EntityEnabledEvent(Entity entity)
{
    /// <summary>
    /// The deactivated entity.
    /// </summary>
    public readonly Entity Entity = entity;
}


