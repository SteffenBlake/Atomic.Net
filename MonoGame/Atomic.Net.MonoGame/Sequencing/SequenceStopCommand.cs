namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Command to stop a running sequence on an entity.
/// </summary>
public readonly record struct SequenceStopCommand(string SequenceId);
