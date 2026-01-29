namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Tracks the execution state of a sequence instance.
/// </summary>
public readonly record struct SequenceState(
    byte StepIndex,
    float StepElapsedTime
);
