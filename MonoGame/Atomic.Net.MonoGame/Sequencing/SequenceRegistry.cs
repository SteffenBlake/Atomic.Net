using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Central registry for sequence lifecycle management.
/// Manages Global vs Scene partition allocation similar to EntityRegistry and RuleRegistry.
/// Provides dual lookup: ID → index and index → sequence.
/// Thread-safe for async scene loading using ConcurrentDictionary and Interlocked.
/// </summary>
public class SequenceRegistry :
    ISingleton<SequenceRegistry>,
    IEventHandler<InitializeEvent>,
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
    
    // Thread-safe for async scene loading
    private readonly ConcurrentDictionary<string, PartitionIndex> _idToIndex = new(
        StringComparer.OrdinalIgnoreCase
    );
    
    // Thread-safe counters for async scene loading
    private int _nextSceneSequenceIndex = 0;
    private int _nextGlobalSequenceIndex = 0;

    /// <summary>
    /// Public accessor for sequences PartitionedSparseArray for iteration and testing.
    /// </summary>
    public PartitionedSparseArray<JsonSequence> Sequences => _sequences;

    /// <summary>
    /// Initialize event handler - registers for other events.
    /// </summary>
    public void OnEvent(InitializeEvent e)
    {
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

        if (Interlocked.CompareExchange(ref _nextSceneSequenceIndex, 0, 0) >= Constants.MaxSceneSequences)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Scene sequence capacity exceeded"));
            index = null;
            return false;
        }

        var seqIndex = (uint)Interlocked.Increment(ref _nextSceneSequenceIndex) - 1;
        PartitionIndex sceneIndex = seqIndex;
        
        // Try to add to dictionary (thread-safe first-write-wins)
        if (!_idToIndex.TryAdd(sequence.Id, sceneIndex))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Sequence with ID '{sequence.Id}' already exists"
            ));
            index = null;
            return false;
        }
        
        _sequences.Set(sceneIndex, sequence);
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

        // Allocate next global sequence index (thread-safe)
        var seqIndex = (ushort)(Interlocked.Increment(ref _nextGlobalSequenceIndex) - 1);
        PartitionIndex globalIndex = seqIndex;
        
        // Try to add to dictionary (thread-safe first-write-wins)
        if (!_idToIndex.TryAdd(sequence.Id, globalIndex))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Sequence with ID '{sequence.Id}' already exists in global partition"
            ));
            index = null;
            return false;
        }

        _sequences.Set(globalIndex, sequence);
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
    /// Resets scene partition by clearing only scene sequences.
    /// Called by ResetDriver during scene transitions.
    /// </summary>
    public void Reset()
    {
        // Clear scene partition only - remove IDs from dictionary
        foreach (var (_, sequence) in _sequences.Scene)
        {
            _idToIndex.TryRemove(sequence.Id, out _);
        }

        // Clear scene partition with O(1) operation
        _sequences.Scene.Clear();

        // Reset scene index allocator (thread-safe)
        Interlocked.Exchange(ref _nextSceneSequenceIndex, 0);
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
