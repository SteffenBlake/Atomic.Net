namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Defines a data-driven sequence of steps for animation and effects.
/// </summary>
public readonly record struct JsonSequence(string Id, JsonSequenceStep[] Steps);
