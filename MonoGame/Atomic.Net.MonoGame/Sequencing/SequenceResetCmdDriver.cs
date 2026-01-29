using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Driver for executing sequence-reset commands.
/// </summary>
public static class SequenceResetCmdDriver
{
    /// <summary>
    /// Executes a sequence-reset command on an entity.
    /// </summary>
    public static void Execute(ushort entityIndex, string sequenceId)
    {
        if (!SequenceRegistry.Instance.TryResolveById(sequenceId, out var sequenceIndex))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Sequence with ID '{sequenceId}' not found"
            ));
            return;
        }

        // Resetting a non-running sequence is a no-op (not an error)
        SequenceDriver.Instance.ResetSequence(entityIndex, sequenceIndex);
    }
}
