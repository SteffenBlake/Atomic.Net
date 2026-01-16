using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the local rotation input for an entity.
/// </summary>
public readonly record struct RotationBehavior(BackedQuaternion Value);

