using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the local scale input for an entity.
/// </summary>
public readonly record struct ScaleBehavior(BackedVector3 Value);

