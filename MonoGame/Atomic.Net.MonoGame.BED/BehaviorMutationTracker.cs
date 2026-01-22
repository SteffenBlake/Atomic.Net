using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED;

/// <summary>
/// Global callback mechanism for tracking entity mutations across all behavior types.
/// Used by DatabaseRegistry to mark entities dirty without creating circular dependencies.
/// </summary>
/// <remarks>
/// senior-dev: This avoids circular dependency between BED and Persistence projects.
/// Persistence registers a callback here, and BehaviorRegistry invokes it on mutations.
/// </remarks>
public static class BehaviorMutationTracker
{
    /// <summary>
    /// Optional callback invoked when any behavior is mutated.
    /// Set by DatabaseRegistry to track dirty entities for disk persistence.
    /// </summary>
    public static Action<ushort>? OnEntityMutated { get; set; }
}
