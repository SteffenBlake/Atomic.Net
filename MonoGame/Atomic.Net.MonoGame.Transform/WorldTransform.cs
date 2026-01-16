using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the final world transform of an entity, calculated from inputs and parent hierarchy.
/// </summary>
public readonly record struct WorldTransform(BackedMatrix? Value);


