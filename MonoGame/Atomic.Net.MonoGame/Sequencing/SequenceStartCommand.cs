namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Command to start a sequence on an entity.
/// </summary>
public readonly record struct SequenceStartCommand(string SequenceId);
