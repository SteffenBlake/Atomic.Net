namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Command to stop a running sequence by ID on the target entity.
/// </summary>
public readonly record struct SequenceStopCommand(string SequenceId);
