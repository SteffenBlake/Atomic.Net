namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Represents a named sequence of steps that can be triggered by rules.
/// </summary>
public readonly record struct JsonSequence(
    string Id,
    JsonSequenceStep[] Steps
);
