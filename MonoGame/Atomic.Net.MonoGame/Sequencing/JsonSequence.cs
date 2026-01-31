namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// JSON representation of a sequence - a named series of steps that execute over time.
/// </summary>
public readonly record struct JsonSequence(
    string Id,
    JsonSequenceStep[] Steps
);
