namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Command to reset a sequence on an entity, restarting it from the beginning.
/// </summary>
public readonly record struct SequenceResetCommand(string SequenceId);
