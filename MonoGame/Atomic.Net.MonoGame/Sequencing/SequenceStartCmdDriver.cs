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
    public static void Execute(ushort entityIndex, string sequenceId)
    {
        if (!SequenceRegistry.Instance.TryResolveById(sequenceId, out var sequenceIndex))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Sequence with ID '{sequenceId}' not found"
            ));
            return;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Cannot start sequence '{sequenceId}' on inactive entity {entityIndex}"
            ));
            return;
        }

        SequenceDriver.Instance.StartSequence(entityIndex, sequenceIndex);
    }
}
