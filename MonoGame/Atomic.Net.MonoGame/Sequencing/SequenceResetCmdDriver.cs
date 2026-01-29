using Atomic.Net.MonoGame.BED;
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
    /// <param name="entityIndex">The entity to reset the sequence on.</param>
    /// <param name="sequenceId">The ID of the sequence to reset.</param>
    public static void Execute(ushort entityIndex, string sequenceId)
    {
        if (!SequenceRegistry.Instance.TryResolveById(sequenceId, out var sequenceIndex))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Sequence with ID '{sequenceId}' not found"
            ));
            return;
        }

        SequenceDriver.Instance.ResetSequence(entityIndex, sequenceIndex);
    }
}
