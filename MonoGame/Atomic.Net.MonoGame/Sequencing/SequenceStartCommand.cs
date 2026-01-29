namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Command to start a sequence by ID on the target entity.
/// </summary>
public readonly record struct SequenceStartCommand(string SequenceId);
