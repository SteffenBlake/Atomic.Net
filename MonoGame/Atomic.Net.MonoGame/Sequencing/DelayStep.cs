namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Pauses sequence execution for a specified duration.
/// </summary>
public readonly record struct DelayStep(float Duration);
