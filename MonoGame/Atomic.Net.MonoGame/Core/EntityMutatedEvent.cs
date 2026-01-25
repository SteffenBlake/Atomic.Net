namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Event fired when any behavior on an entity is added or updated.
/// Used by DatabaseRegistry to track dirty entities for disk persistence.
/// </summary>
public readonly record struct EntityMutatedEvent(Entity Entity);
