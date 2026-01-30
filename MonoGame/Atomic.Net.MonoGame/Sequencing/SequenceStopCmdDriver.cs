using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Executes sequence-stop commands by stopping running sequences on entities.
/// </summary>
public sealed class SequenceStopCmdDriver : ISingleton<SequenceStopCmdDriver>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = new();
    }

    public static SequenceStopCmdDriver Instance { get; private set; } = null!;

    /// <summary>
    /// Stop a running sequence on an entity.
    /// </summary>
    /// <param name="entityIndex">The entity index.</param>
    /// <param name="sequenceId">The sequence ID to stop.</param>
    public void Execute(ushort entityIndex, string sequenceId)
    {
        // Resolve sequence ID to index
        if (!SequenceRegistry.Instance.TryResolveById(sequenceId, out var sequenceIndex))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Sequence with ID '{sequenceId}' not found"
            ));
            return;
        }

        // Delegate to SequenceDriver (ignores no-op cases internally)
        SequenceDriver.Instance.StopSequence(entityIndex, sequenceIndex);
    }
}
