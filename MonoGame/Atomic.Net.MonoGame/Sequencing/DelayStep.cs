namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Sequence step that pauses execution for a specified duration.
/// </summary>
public readonly record struct DelayStep(float Duration);
