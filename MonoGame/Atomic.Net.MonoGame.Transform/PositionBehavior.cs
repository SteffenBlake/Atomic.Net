using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the local position input for an entity.
/// </summary>
public readonly record struct PositionBehavior(BackedVector3? Value);

