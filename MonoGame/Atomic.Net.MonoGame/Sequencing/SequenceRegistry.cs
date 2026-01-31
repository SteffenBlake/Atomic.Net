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
    /// Initialize event handler - registers for other events.
    /// </summary>
    public void OnEvent(InitializeEvent e)
    {
        EventBus<ResetEvent>.Register(this);
        EventBus<ShutdownEvent>.Register(this);
    }

    /// <summary>
    /// Activate the next available scene sequence (index greater than or equal to MaxGlobalSequences).
    /// </summary>
    /// <param name="sequence">The sequence to activate.</param>
    /// <returns>The index of the activated sequence, or ushort.MaxValue on error.</returns>
    public ushort Activate(JsonSequence sequence)
    {
        if (string.IsNullOrWhiteSpace(sequence.Id))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Sequence ID cannot be null or empty"));
            return ushort.MaxValue;
        }

        // Validate sequence steps
        if (!ValidateSequence(sequence))
        {
            return ushort.MaxValue;
        }

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
        if (string.IsNullOrWhiteSpace(sequence.Id))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Sequence ID cannot be null or empty"));
            return ushort.MaxValue;
        }

        // Validate sequence steps
        if (!ValidateSequence(sequence))
        {
            return ushort.MaxValue;
        }

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
