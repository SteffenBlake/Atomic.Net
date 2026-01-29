namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Command to reset a running sequence by ID on the target entity (restart from beginning).
/// </summary>
public readonly record struct SequenceResetCommand(string SequenceId);
