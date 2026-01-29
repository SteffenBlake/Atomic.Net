namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Runtime state of a sequence instance.
/// Tracks which step is executing and elapsed time in that step.
/// </summary>
public readonly record struct SequenceState(ushort StepIndex, float StepElapsedTime);
