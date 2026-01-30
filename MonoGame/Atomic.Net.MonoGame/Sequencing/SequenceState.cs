namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Tracks the runtime state of a sequence instance.
/// Stores current step index and elapsed time within that step.
/// </summary>
public readonly record struct SequenceState(
    byte StepIndex,
    float StepElapsedTime
);
