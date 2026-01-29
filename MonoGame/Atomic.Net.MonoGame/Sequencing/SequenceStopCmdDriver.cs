using Atomic.Net.MonoGame.BED;
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
    /// <param name="entityIndex">The entity to stop the sequence on.</param>
    /// <param name="sequenceId">The ID of the sequence to stop.</param>
    public static void Execute(ushort entityIndex, string sequenceId)
    {
        if (!SequenceRegistry.Instance.TryResolveById(sequenceId, out var sequenceIndex))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Sequence with ID '{sequenceId}' not found"
            ));
            return;
        }

        SequenceDriver.Instance.StopSequence(entityIndex, sequenceIndex);
    }
}
