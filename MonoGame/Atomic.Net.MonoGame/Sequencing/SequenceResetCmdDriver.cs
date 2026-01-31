using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Executes sequence-reset commands by resetting running sequences on entities.
/// </summary>
public sealed class SequenceResetCmdDriver : ISingleton<SequenceResetCmdDriver>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = new();
    }

    public static SequenceResetCmdDriver Instance { get; private set; } = null!;

    /// <summary>
    /// Reset a running sequence on an entity back to its first step.
    /// </summary>
    /// <param name="entityIndex">The entity index.</param>
    /// <param name="sequenceId">The sequence ID to reset.</param>
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
        SequenceDriver.Instance.ResetSequence(entityIndex, sequenceIndex);
    }
}
