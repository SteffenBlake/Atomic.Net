using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Driver for executing sequence-start commands.
/// </summary>
public static class SequenceStartCmdDriver
{
    /// <summary>
    /// Executes a sequence-start command on an entity.
    /// </summary>
    /// <param name="entityIndex">The entity to start the sequence on.</param>
    /// <param name="sequenceId">The ID of the sequence to start.</param>
    public static void Execute(ushort entityIndex, string sequenceId)
    {
        if (!EntityRegistry.Instance.IsActive(new Entity(entityIndex)))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Cannot start sequence '{sequenceId}' on inactive entity {entityIndex}"
            ));
            return;
        }

        if (!SequenceRegistry.Instance.TryResolveById(sequenceId, out var sequenceIndex))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Sequence with ID '{sequenceId}' not found"
            ));
            return;
        }

        SequenceDriver.Instance.StartSequence(entityIndex, sequenceIndex);
    }
}
