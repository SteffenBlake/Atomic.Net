using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Singleton that processes all active sequences per-frame.
/// Tracks sequence state per entity and executes steps based on elapsed time.
/// </summary>
public sealed class SequenceDriver : 
    ISingleton<SequenceDriver>,
    IEventHandler<PreEntityDeactivatedEvent>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = new();
        EventBus<PreEntityDeactivatedEvent>.Register(Instance);
    }

    public static SequenceDriver Instance { get; private set; } = null!;

    // CRITICAL: Inner array must be sized to MaxSequences (512) because we index by
    // global sequence index, not by entity's active sequence slot number
    private readonly SparseReferenceArray<SparseArray<SequenceState>> _activeSequences = 
        new(Constants.MaxEntities);

    // Pre-allocated context objects to avoid allocations in hot path
    private readonly JsonObject _doContext = new()
    {
        ["world"] = new JsonObject { ["deltaTime"] = 0.0f },
        ["self"] = null
    };

    private readonly JsonObject _tweenContext = new()
    {
        ["tween"] = 0.0f,
        ["self"] = null
    };

    private readonly JsonObject _repeatContext = new()
    {
        ["elapsed"] = 0.0f,
        ["self"] = null
    };

    // Pre-allocated lists for tracking removals (zero-alloc after init)
    private readonly List<ushort> _sequencesToRemove = new(Constants.MaxSequences);
    private readonly List<ushort> _entitiesToRemove = new(Constants.MaxEntities);

    /// <summary>
    /// Main entry point called per-frame to process all active sequences.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds</param>
    public void RunFrame(float deltaTime)
    {
        _entitiesToRemove.Clear();

        foreach (var (entityIndex, sequenceStates) in _activeSequences)
        {
            var entity = EntityRegistry.Instance[entityIndex];
            if (!entity.Active)
            {
                continue;
            }

            var entityJsonNode = JsonEntityConverter.Read(entity);
            _sequencesToRemove.Clear();

            foreach (var (sequenceIndex, sequenceState) in sequenceStates)
            {
                if (!SequenceRegistry.Instance.Sequences.TryGetValue(sequenceIndex, out var sequenceNullable))
                {
                    _sequencesToRemove.Add(sequenceIndex);
                    continue;
                }
                
                var sequence = sequenceNullable.Value;

                var shouldRemove = ProcessSequence(
                    sequenceIndex,
                    sequence,
                    sequenceState,
                    deltaTime,
                    entityJsonNode,
                    sequenceStates
                );

                if (shouldRemove)
                {
                    _sequencesToRemove.Add(sequenceIndex);
                }
            }

            foreach (var sequenceIndex in _sequencesToRemove)
            {
                sequenceStates.Remove(sequenceIndex);
            }

            if (sequenceStates.Count == 0)
            {
                _entitiesToRemove.Add(entityIndex);
            }

            JsonEntityConverter.Write(entityJsonNode, entity);
        }

        // Remove entities with no active sequences after iteration
        foreach (var entityIndex in _entitiesToRemove)
        {
            _activeSequences.Remove(entityIndex);
        }
    }

    /// <summary>
    /// Process a single sequence instance, updating its state and executing steps.
    /// Steps can complete in the same frame, with remaining time carried forward.
    /// Returns true if the sequence should be removed (completed).
    /// </summary>
    private bool ProcessSequence(
        ushort sequenceIndex,
        JsonSequence sequence,
        SequenceState state,
        float deltaTime,
        JsonNode entityJsonNode,
        SparseArray<SequenceState> sequenceStates
    )
    {
        var currentStepIndex = state.StepIndex;
        var remainingTime = deltaTime;

        while (currentStepIndex < sequence.Steps.Length && remainingTime > 0)
        {
            var step = sequence.Steps[currentStepIndex];

            if (step.TryMatch(out DelayStep delayStep))
            {
                var stepRemaining = delayStep.Duration - state.StepElapsedTime;

                if (stepRemaining <= remainingTime)
                {
                    remainingTime -= stepRemaining;
                    currentStepIndex++;
                }
                else
                {
                    state = state with { 
                        StepElapsedTime = state.StepElapsedTime + remainingTime 
                    };
                    remainingTime = 0;
                }
            }
            else if (step.TryMatch(out DoStep doStep))
            {
                // Build context and execute
                _doContext.Remove("self");
                _doContext["world"]!["deltaTime"] = remainingTime;
                _doContext["self"] = entityJsonNode;
                doStep.Do.Execute(entityJsonNode, _doContext);
                _doContext.Remove("self");  // Clean up immediately after use
                
                currentStepIndex++;
                // Do NOT consume remaining time - allow progression to next step in same frame
            }
            else if (step.TryMatch(out TweenStep tweenStep))
            {
                var delta = (state.StepElapsedTime + remainingTime) / tweenStep.Duration;
                delta = MathF.Min(1.0f, delta);
                var tween = tweenStep.From + (tweenStep.To - tweenStep.From) * delta;
                _tweenContext.Remove("self");
                _tweenContext["tween"] = tween;
                _tweenContext["self"] = entityJsonNode;

                tweenStep.Do.Execute(entityJsonNode, _tweenContext);

                // Clean up immediately after use
                _tweenContext.Remove("self");  

                var stepRemaining = tweenStep.Duration - state.StepElapsedTime;
                if (stepRemaining <= remainingTime)
                {
                    remainingTime -= stepRemaining;
                    currentStepIndex++;
                }
                else
                {
                    state = state with { 
                        StepElapsedTime = state.StepElapsedTime + remainingTime 
                    };
                    remainingTime = 0;
                }
            }
            else if (step.TryMatch(out RepeatStep repeatStep))
            {
                var conditionMet = false;
                var elapsed = state.StepElapsedTime;
                _repeatContext["self"] = entityJsonNode;

                while (conditionMet == false && remainingTime >= repeatStep.Every)
                {
                    remainingTime -= repeatStep.Every;
                    elapsed += repeatStep.Every;
                    _repeatContext["elapsed"] = elapsed;

                    repeatStep.Do.Execute(entityJsonNode, _repeatContext);

                    if (!repeatStep.Until.TryEvaluateCondition(_repeatContext, out conditionMet))
                    {
                        EventBus<ErrorEvent>.Push(new ErrorEvent(
                            $"Unable to evaluate bool condition from logic: {repeatStep.Until.ToJsonString()}"
                        ));

                        // Clean up immediately after use
                        _repeatContext.Remove("self");  
                        // something went wrong, mark this sequence to be removed
                        return true; 
                    }
                }

                // Clean up immediately after use
                _repeatContext.Remove("self");  

                if (conditionMet)
                {
                    currentStepIndex++;
                }
                else
                {
                    state = state with { 
                        StepElapsedTime = elapsed + remainingTime 
                    };
                    remainingTime = 0;
                }
            }
            else
            {
                // Unknown step type - push error
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Unknown sequence step type at step {currentStepIndex} in sequence {sequenceIndex}"
                ));

                // something went wrong, mark this sequence to be removed
                return true;
            }
        }

        if (currentStepIndex >= sequence.Steps.Length)
        {
            // Sequence complete, should be removed
            return true; 
        }
        
        // Update sequence state
        sequenceStates.Set(sequenceIndex, state);

        // Sequence still running
        return false; 
    }

    /// <summary>
    /// Start a sequence on an entity.
    /// </summary>
    public void StartSequence(ushort entityIndex, ushort sequenceIndex)
    {
        if (!_activeSequences.TryGetValue(entityIndex, out var sequenceStates))
        {
            // CRITICAL: Size must be MaxSequences (512) because we index by global sequence index
            sequenceStates = new SparseArray<SequenceState>(Constants.MaxSequences);
            _activeSequences[entityIndex] = sequenceStates;
        }

        if (sequenceStates.HasValue(sequenceIndex))
        {
            return;
        }

        sequenceStates.Set(sequenceIndex, new SequenceState(0, 0));
    }

    /// <summary>
    /// Stop a running sequence on an entity.
    /// </summary>
    public void StopSequence(ushort entityIndex, ushort sequenceIndex)
    {
        if (!_activeSequences.TryGetValue(entityIndex, out var sequenceStates))
        {
            return;
        }

        sequenceStates.Remove(sequenceIndex);

        if (sequenceStates.Count == 0)
        {
            _activeSequences.Remove(entityIndex);
        }
    }

    /// <summary>
    /// Reset a running sequence on an entity back to step 0.
    /// If the sequence is not running, starts it.
    /// </summary>
    public void ResetSequence(ushort entityIndex, ushort sequenceIndex)
    {
        if (!_activeSequences.TryGetValue(entityIndex, out var sequenceStates))
        {
            sequenceStates = new SparseArray<SequenceState>(Constants.MaxSequences);
            _activeSequences[entityIndex] = sequenceStates;
        }

        // Always set to initial state (either reset or start)
        sequenceStates.Set(sequenceIndex, new SequenceState(0, 0));
    }

    /// <summary>
    /// Handle entity deactivation by removing all sequences for that entity.
    /// </summary>
    public void OnEvent(PreEntityDeactivatedEvent e)
    {
        _activeSequences.Remove(e.Entity.Index);
    }
}
