using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Driver for executing sequence-stop commands.
/// </summary>
public static class SequenceStopCmdDriver
{
    /// <summary>
    /// Executes a sequence-stop command on an entity.
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

        // Stopping a non-running sequence is a no-op (not an error)
        SequenceDriver.Instance.StopSequence(entityIndex, sequenceIndex);
    }
}
