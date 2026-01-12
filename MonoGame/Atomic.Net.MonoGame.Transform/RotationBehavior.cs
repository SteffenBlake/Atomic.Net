using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the local rotation input for an entity.
/// </summary>
public readonly record struct RotationBehavior(Quaternion Value);


