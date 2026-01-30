using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Central registry for sequence lifecycle management.
/// Manages Global vs Scene partition allocation similar to EntityRegistry and RuleRegistry.
/// Provides dual lookup: ID → index and index → sequence.
/// </summary>
public class SequenceRegistry : IEventHandler<ResetEvent>, IEventHandler<ShutdownEvent>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
        EventBus<ResetEvent>.Register(Instance);
        EventBus<ShutdownEvent>.Register(Instance);
    }

    public static SequenceRegistry Instance { get; private set; } = null!;

    private readonly SparseArray<JsonSequence> _sequences = new(Constants.MaxSequences);
    private readonly Dictionary<string, ushort> _idToIndex = new(
        Constants.MaxSequences,
        StringComparer.OrdinalIgnoreCase
    );
    private ushort _nextSceneSequenceIndex = Constants.MaxGlobalSequences;
    private ushort _nextGlobalSequenceIndex = 0;

    /// <summary>
    /// Public accessor for sequences SparseArray for iteration and testing.
    /// </summary>
    public SparseArray<JsonSequence> Sequences => _sequences;

    /// <summary>
    /// Activate the next available scene sequence (index greater than or equal to MaxGlobalSequences).
    /// </summary>
    /// <param name="sequence">The sequence to activate.</param>
    /// <returns>The index of the activated sequence, or ushort.MaxValue on error.</returns>
    public ushort Activate(JsonSequence sequence)
    {
        if (_nextSceneSequenceIndex >= Constants.MaxSequences)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Scene sequence capacity exceeded"));
            return ushort.MaxValue;
        }

        // Check for duplicate ID in scene partition
        if (_idToIndex.ContainsKey(sequence.Id))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Sequence with ID '{sequence.Id}' already exists"
            ));
            return ushort.MaxValue;
        }

        var index = _nextSceneSequenceIndex++;
        _sequences.Set(index, sequence);
        _idToIndex[sequence.Id] = index;
        return index;
    }

    /// <summary>
    /// Activate the next available global sequence (index less than MaxGlobalSequences).
    /// </summary>
    /// <param name="sequence">The sequence to activate.</param>
    /// <returns>The index of the activated sequence, or ushort.MaxValue on error.</returns>
    public ushort ActivateGlobal(JsonSequence sequence)
    {
        if (_nextGlobalSequenceIndex >= Constants.MaxGlobalSequences)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Global sequence capacity exceeded"));
            return ushort.MaxValue;
        }

        // Check for duplicate ID in global partition
        if (_idToIndex.ContainsKey(sequence.Id))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Sequence with ID '{sequence.Id}' already exists"
            ));
            return ushort.MaxValue;
        }

        var index = _nextGlobalSequenceIndex++;
        _sequences.Set(index, sequence);
        _idToIndex[sequence.Id] = index;
        return index;
    }

    /// <summary>
    /// Try to resolve a sequence by its index.
    /// </summary>
    /// <param name="index">The sequence index.</param>
    /// <param name="sequence">The resolved sequence.</param>
    /// <returns>True if the sequence was found.</returns>
    public bool TryResolveByIndex(ushort index, out JsonSequence sequence)
    {
        if (_sequences.TryGetValue(index, out var seq))
        {
            sequence = seq.Value;
            return true;
        }

        sequence = default;
        return false;
    }

    /// <summary>
    /// Try to resolve a sequence index by its ID.
    /// </summary>
    /// <param name="id">The sequence ID.</param>
    /// <param name="index">The resolved index.</param>
    /// <returns>True if the sequence was found.</returns>
    public bool TryResolveById(string id, out ushort index)
    {
        return _idToIndex.TryGetValue(id, out index);
    }

    /// <summary>
    /// Handle reset event by deactivating only scene sequences.
    /// </summary>
    public void OnEvent(ResetEvent _)
    {
        // Clear scene partition only (>= MaxGlobalSequences)
        for (ushort i = Constants.MaxGlobalSequences; i < Constants.MaxSequences; i++)
        {
            if (_sequences.TryGetValue(i, out var sequence))
            {
                _idToIndex.Remove(sequence.Value.Id);
                _sequences.Remove(i);
            }
        }

        // Reset scene index allocator
        _nextSceneSequenceIndex = Constants.MaxGlobalSequences;
    }

    /// <summary>
    /// Handle shutdown event by deactivating ALL sequences (both global and scene).
    /// Used for complete game shutdown and test cleanup.
    /// </summary>
    public void OnEvent(ShutdownEvent _)
    {
        // Clear all partitions (both global and scene)
        _sequences.Clear();
        _idToIndex.Clear();

        // Reset both index allocators
        _nextGlobalSequenceIndex = 0;
        _nextSceneSequenceIndex = Constants.MaxGlobalSequences;
    }
}
