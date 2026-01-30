namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Command to reset a running sequence on an entity back to its first step.
/// </summary>
public readonly record struct SequenceResetCommand(string SequenceId);
