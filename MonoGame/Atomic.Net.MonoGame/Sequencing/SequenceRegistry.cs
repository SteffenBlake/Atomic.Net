using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Central registry for sequence lifecycle management.
/// Manages Global vs Scene partition allocation similar to RuleRegistry.
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
    private readonly Dictionary<string, ushort> _idToIndex = new(Constants.MaxSequences);
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
        // senior-dev: Allocate from scene partition (>= MaxGlobalSequences)
        if (_nextSceneSequenceIndex >= Constants.MaxSequences)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Scene sequence capacity exceeded"));
            return ushort.MaxValue;
        }

        // senior-dev: Enforce unique IDs within scene partition
        if (_idToIndex.ContainsKey(sequence.Id))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Sequence with ID '{sequence.Id}' already exists"));
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
        // senior-dev: Allocate from global partition (< MaxGlobalSequences)
        if (_nextGlobalSequenceIndex >= Constants.MaxGlobalSequences)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Global sequence capacity exceeded"));
            return ushort.MaxValue;
        }

        // senior-dev: Enforce unique IDs within global partition
        if (_idToIndex.ContainsKey(sequence.Id))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Sequence with ID '{sequence.Id}' already exists"));
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
    public bool TryResolveByIndex(ushort index, out JsonSequence sequence)
    {
        if (_sequences.HasValue(index))
        {
            sequence = _sequences[index];
            return true;
        }

        sequence = default;
        return false;
    }

    /// <summary>
    /// Try to resolve a sequence index by its string ID.
    /// </summary>
    public bool TryResolveById(string id, out ushort index)
    {
        return _idToIndex.TryGetValue(id, out index);
    }

    /// <summary>
    /// Handle reset event by deactivating only scene sequences.
    /// </summary>
    public void OnEvent(ResetEvent _)
    {
        // senior-dev: Clear scene partition only (>= MaxGlobalSequences)
        for (ushort i = Constants.MaxGlobalSequences; i < Constants.MaxSequences; i++)
        {
            if (_sequences.HasValue(i))
            {
                var sequence = _sequences[i];
                _idToIndex.Remove(sequence.Id);
                _sequences.Remove(i);
            }
        }

        // senior-dev: Reset scene index allocator
        _nextSceneSequenceIndex = Constants.MaxGlobalSequences;
    }

    /// <summary>
    /// Handle shutdown event by deactivating ALL sequences (both global and scene).
    /// Used for complete game shutdown and test cleanup.
    /// </summary>
    public void OnEvent(ShutdownEvent _)
    {
        // senior-dev: Clear all partitions (both global and scene)
        _sequences.Clear();
        _idToIndex.Clear();

        // senior-dev: Reset both index allocators
        _nextGlobalSequenceIndex = 0;
        _nextSceneSequenceIndex = Constants.MaxGlobalSequences;
    }
}
