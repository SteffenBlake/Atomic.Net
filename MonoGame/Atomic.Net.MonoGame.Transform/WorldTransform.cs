using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the final world transform of an entity, calculated from inputs and parent hierarchy.
/// </summary>
public readonly record struct WorldTransform(
    Matrix Transform
);


