using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Executes sequence-start commands by starting sequences on entities.
/// </summary>
public sealed class SequenceStartCmdDriver : ISingleton<SequenceStartCmdDriver>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = new();
    }

    public static SequenceStartCmdDriver Instance { get; private set; } = null!;

    /// <summary>
    /// Start a sequence on an entity.
    /// </summary>
    /// <param name="entityIndex">The entity index.</param>
    /// <param name="sequenceId">The sequence ID to start.</param>
    public void Execute(PartitionIndex entityIndex, string sequenceId)
    {
        // Validate entity is active
        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Cannot start sequence on inactive entity {entityIndex}"
            ));
            return;
        }

        // Resolve sequence ID to index
        if (!SequenceRegistry.Instance.TryResolveById(sequenceId, out var sequenceIndex))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Sequence with ID '{sequenceId}' not found"
            ));
            return;
        }

        // Delegate to SequenceDriver
        SequenceDriver.Instance.StartSequence(entityIndex, sequenceIndex);
    }
}
