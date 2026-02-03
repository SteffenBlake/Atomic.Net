using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Central registry for sequence lifecycle management.
/// Manages Global vs Scene partition allocation similar to EntityRegistry and RuleRegistry.
/// Provides dual lookup: ID → index and index → sequence.
/// </summary>
public class SequenceRegistry : 
    ISingleton<SequenceRegistry>,
    IEventHandler<InitializeEvent>,
    IEventHandler<ResetEvent>, 
    IEventHandler<ShutdownEvent>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = new();
        EventBus<InitializeEvent>.Register(Instance);
    }

    public static SequenceRegistry Instance { get; private set; } = null!;

    private readonly PartitionedSparseArray<JsonSequence> _sequences = new(
        Constants.MaxGlobalSequences,
        Constants.MaxSceneSequences
    );
    private readonly Dictionary<string, PartitionIndex> _idToIndex = new(
        (int)(Constants.MaxGlobalSequences + Constants.MaxSceneSequences),
        StringComparer.OrdinalIgnoreCase
    );
    private uint _nextSceneSequenceIndex = 0;
    private ushort _nextGlobalSequenceIndex = 0;

    /// <summary>
    /// Public accessor for sequences PartitionedSparseArray for iteration and testing.
    /// </summary>
    public PartitionedSparseArray<JsonSequence> Sequences => _sequences;

    /// <summary>
    /// Initialize event handler - registers for other events.
    /// </summary>
    public void OnEvent(InitializeEvent e)
    {
        EventBus<ResetEvent>.Register(this);
        EventBus<ShutdownEvent>.Register(this);
    }

    /// <summary>
    /// Tries to activate the next available scene sequence.
    /// </summary>
    /// <param name="sequence">The sequence to activate.</param>
    /// <param name="index">The partition index of the activated sequence, if successful.</param>
    /// <returns>True if activation succeeded; false otherwise.</returns>
    public bool TryActivate(JsonSequence sequence, [NotNullWhen(true)] out PartitionIndex? index)
    {
        if (string.IsNullOrWhiteSpace(sequence.Id))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Sequence ID cannot be null or empty"));
            index = null;
            return false;
        }

        // Validate sequence steps
        if (!ValidateSequence(sequence))
        {
            index = null;
            return false;
        }

        if (_nextSceneSequenceIndex >= Constants.MaxSceneSequences)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Scene sequence capacity exceeded"));
            index = null;
            return false;
        }

        // Check for duplicate ID in scene partition
        if (_idToIndex.ContainsKey(sequence.Id))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Sequence with ID '{sequence.Id}' already exists"
            ));
            index = null;
            return false;
        }

        var seqIndex = _nextSceneSequenceIndex++;
        PartitionIndex sceneIndex = seqIndex;
        _sequences.Set(sceneIndex, sequence);
        _idToIndex[sequence.Id] = sceneIndex;
        index = sceneIndex;
        return true;
    }

    /// <summary>
    /// Tries to activate the next available global sequence.
    /// </summary>
    /// <param name="sequence">The sequence to activate.</param>
    /// <param name="index">The partition index of the activated sequence, if successful.</param>
    /// <returns>True if activation succeeded; false otherwise.</returns>
    public bool TryActivateGlobal(JsonSequence sequence, [NotNullWhen(true)] out PartitionIndex? index)
    {
        if (string.IsNullOrWhiteSpace(sequence.Id))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Sequence ID cannot be null or empty"));
            index = null;
            return false;
        }

        // Validate sequence steps
        if (!ValidateSequence(sequence))
        {
            index = null;
            return false;
        }

        if (_nextGlobalSequenceIndex >= Constants.MaxGlobalSequences)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Global sequence capacity exceeded"));
            index = null;
            return false;
        }

        // Check for duplicate ID in global partition
        if (_idToIndex.ContainsKey(sequence.Id))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Sequence with ID '{sequence.Id}' already exists"
            ));
            index = null;
            return false;
        }

        var seqIndex = _nextGlobalSequenceIndex++;
        PartitionIndex globalIndex = seqIndex;
        _sequences.Set(globalIndex, sequence);
        _idToIndex[sequence.Id] = globalIndex;
        index = globalIndex;
        return true;
    }

    /// <summary>
    /// Validates that a sequence has valid steps with required fields.
    /// Pushes ErrorEvent for any validation failures.
    /// </summary>
    private bool ValidateSequence(JsonSequence sequence)
    {
        if (sequence.Steps == null || sequence.Steps.Length == 0)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Sequence '{sequence.Id}' has no steps"
            ));
            return false;
        }

        for (int i = 0; i < sequence.Steps.Length; i++)
        {
            var step = sequence.Steps[i];
            
            if (step.TryMatch(out DelayStep delayStep))
            {
                if (delayStep.Duration <= 0)
                {
                    EventBus<ErrorEvent>.Push(new ErrorEvent(
                        $"Sequence '{sequence.Id}' step {i}: Delay duration must be greater than 0"
                    ));
                    return false;
                }
            }
            else if (step.TryMatch(out DoStep doStep))
            {
                // DoStep contains a SceneCommand which has its own validation
                // If it deserialized, it's valid enough
            }
            else if (step.TryMatch(out TweenStep tweenStep))
            {
                if (tweenStep.Duration <= 0)
                {
                    EventBus<ErrorEvent>.Push(new ErrorEvent(
                        $"Sequence '{sequence.Id}' step {i}: Tween duration must be greater than 0"
                    ));
                    return false;
                }
            }
            else if (step.TryMatch(out RepeatStep repeatStep))
            {
                if (repeatStep.Every <= 0)
                {
                    EventBus<ErrorEvent>.Push(new ErrorEvent(
                        $"Sequence '{sequence.Id}' step {i}: Repeat interval must be greater than 0"
                    ));
                    return false;
                }
            }
            else
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Sequence '{sequence.Id}' step {i}: Unknown step type"
                ));
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Try to resolve a sequence index by its ID.
    /// </summary>
    /// <param name="id">The sequence ID.</param>
    /// <param name="index">The resolved index.</param>
    /// <returns>True if the sequence was found.</returns>
    public bool TryResolveById(string id, out PartitionIndex index)
    {
        if (_idToIndex.TryGetValue(id, out index))
        {
            return true;
        }
        index = default;
        return false;
    }

    /// <summary>
    /// Handle reset event by deactivating only scene sequences.
    /// </summary>
    public void OnEvent(ResetEvent _)
    {
        // Clear scene partition only - remove IDs from dictionary
        foreach (var (_, sequence) in _sequences.Scene)
        {
            _idToIndex.Remove(sequence.Id);
        }
        
        // Clear scene partition with O(1) operation
        _sequences.Scene.Clear();

        // Reset scene index allocator
        _nextSceneSequenceIndex = 0;
    }

    /// <summary>
    /// Handle shutdown event by deactivating ALL sequences (both global and scene).
    /// Used for complete game shutdown and test cleanup.
    /// </summary>
    public void OnEvent(ShutdownEvent _)
    {
        // Clear all partitions
        _sequences.Global.Clear();
        _sequences.Scene.Clear();
        _idToIndex.Clear();

        // Reset both index allocators
        _nextGlobalSequenceIndex = 0;
        _nextSceneSequenceIndex = 0;
    }
}
