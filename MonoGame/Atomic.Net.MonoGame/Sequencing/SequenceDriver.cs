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

    // CRITICAL: Inner array must be PartitionedSparseArray because sequences use PartitionIndex
    private readonly PartitionedSparseRefArray<PartitionedSparseArray<SequenceState>> _activeSequences = 
        new(Constants.MaxGlobalEntities, Constants.MaxSceneEntities);

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
    private readonly List<PartitionIndex> _sequencesToRemove = new((int)(Constants.MaxGlobalSequences + Constants.MaxSceneSequences));
    private readonly List<PartitionIndex> _entitiesToRemove = new((int)(Constants.MaxGlobalEntities + Constants.MaxSceneEntities));

    /// <summary>
    /// Main entry point called per-frame to process all active sequences.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds</param>
    public void RunFrame(float deltaTime)
    {
        _entitiesToRemove.Clear();

        // Process global entities
        foreach (var (entityIndex, sequenceStates) in _activeSequences.Global)
        {
            ProcessEntitySequences((ushort)entityIndex, sequenceStates, deltaTime);
        }

        // Process scene entities
        foreach (var (entityIndex, sequenceStates) in _activeSequences.Scene)
        {
            ProcessEntitySequences((uint)entityIndex, sequenceStates, deltaTime);
        }

        // Remove entities with no active sequences after iteration
        foreach (var entityIndex in _entitiesToRemove)
        {
            _activeSequences.Remove(entityIndex);
        }
    }

    private void ProcessEntitySequences(
        PartitionIndex entityIndex, 
        PartitionedSparseArray<SequenceState> sequenceStates, 
        float deltaTime
    )
    {
        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            return;
        }

        var entityJsonNode = JsonEntityConverter.Read(entity);
        _sequencesToRemove.Clear();

        // Process global sequences
        foreach (var (sequenceIndex, sequenceState) in sequenceStates.Global)
        {
            ProcessSingleSequence((ushort)sequenceIndex, sequenceState, deltaTime, entityJsonNode, sequenceStates);
        }

        // Process scene sequences
        foreach (var (sequenceIndex, sequenceState) in sequenceStates.Scene)
        {
            ProcessSingleSequence((uint)sequenceIndex, sequenceState, deltaTime, entityJsonNode, sequenceStates);
        }

        foreach (var sequenceIndex in _sequencesToRemove)
        {
            sequenceStates.Remove(sequenceIndex);
        }

        if (sequenceStates.Global.Count == 0 && sequenceStates.Scene.Count == 0)
        {
            _entitiesToRemove.Add(entityIndex);
        }

        JsonEntityConverter.Write(entityJsonNode, entity);
    }

    private void ProcessSingleSequence(
        PartitionIndex sequenceIndex,
        SequenceState sequenceState,
        float deltaTime,
        JsonNode entityJsonNode,
        PartitionedSparseArray<SequenceState> sequenceStates
    )
    {
        if (!SequenceRegistry.Instance.Sequences.TryGetValue(sequenceIndex, out var sequenceNullable))
        {
            _sequencesToRemove.Add(sequenceIndex);
            return;
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

    /// <summary>
    /// Process a single sequence instance, updating its state and executing steps.
    /// Steps can complete in the same frame, with remaining time carried forward.
    /// Returns true if the sequence should be removed (completed).
    /// </summary>
    private bool ProcessSequence(
        PartitionIndex sequenceIndex,
        JsonSequence sequence,
        SequenceState state,
        float deltaTime,
        JsonNode entityJsonNode,
        PartitionedSparseArray<SequenceState> sequenceStates
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
    public void StartSequence(PartitionIndex entityIndex, PartitionIndex sequenceIndex)
    {
        if (!_activeSequences.TryGetValue(entityIndex, out var sequenceStates))
        {
            // CRITICAL: Size must cover both global and scene sequences
            sequenceStates = new PartitionedSparseArray<SequenceState>(
                Constants.MaxGlobalSequences,
                Constants.MaxSceneSequences
            );
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
    public void StopSequence(PartitionIndex entityIndex, PartitionIndex sequenceIndex)
    {
        if (!_activeSequences.TryGetValue(entityIndex, out var sequenceStates))
        {
            return;
        }

        sequenceStates.Remove(sequenceIndex);

        if (sequenceStates.Global.Count == 0 && sequenceStates.Scene.Count == 0)
        {
            _activeSequences.Remove(entityIndex);
        }
    }

    /// <summary>
    /// Reset a running sequence on an entity back to step 0.
    /// If the sequence is not running, starts it.
    /// </summary>
    public void ResetSequence(PartitionIndex entityIndex, PartitionIndex sequenceIndex)
    {
        if (!_activeSequences.TryGetValue(entityIndex, out var sequenceStates))
        {
            sequenceStates = new PartitionedSparseArray<SequenceState>(
                Constants.MaxGlobalSequences,
                Constants.MaxSceneSequences
            );
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
