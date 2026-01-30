using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Core.Extensions;
using Atomic.Net.MonoGame.Scenes;
using Json.Logic;

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

    // Outer index: entity, inner index: sequence
    // senior-dev: CRITICAL - Inner array MUST be sized to MaxSequences (512) not MaxActiveSequencesPerEntity (8)
    // because we index by global sequence index, not by "slot number"
    private readonly SparseReferenceArray<SparseArray<SequenceState>> _activeSequences = 
        new(Constants.MaxEntities);

    // Pre-allocated context objects to avoid allocations in hot path
    private readonly JsonObject _doContext = new()
    {
        ["world"] = new JsonObject { ["deltaTime"] = 0.0f },
        ["self"] = null!
    };

    private readonly JsonObject _tweenContext = new()
    {
        ["tween"] = 0.0f,
        ["self"] = null!
    };

    private readonly JsonObject _repeatContext = new()
    {
        ["elapsed"] = 0.0f,
        ["self"] = null!
    };

    // Pre-allocated lists for tracking removals (zero-alloc after init)
    private readonly List<ushort> _sequencesToRemove = new(Constants.MaxActiveSequencesPerEntity);
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
                if (!SequenceRegistry.Instance.TryResolveByIndex(sequenceIndex, out var sequence))
                {
                    _sequencesToRemove.Add(sequenceIndex);
                    continue;
                }

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

            if (!sequenceStates.Any())
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
            var stepElapsedTime = state.StepElapsedTime;

            if (step.TryMatch(out DelayStep delayStep))
            {
                stepElapsedTime += remainingTime;
                if (stepElapsedTime >= delayStep.Duration)
                {
                    remainingTime = stepElapsedTime - delayStep.Duration;
                    currentStepIndex++;
                    stepElapsedTime = 0;
                }
                else
                {
                    remainingTime = 0;
                }
            }
            else if (step.TryMatch(out DoStep doStep))
            {
                // senior-dev: Do steps execute once per frame, consuming the entire frame
                // This prevents cascading do steps in a single frame
                ExecuteCommand(doStep.Do, entityJsonNode, BuildDoContext(entityJsonNode, remainingTime));
                currentStepIndex++;
                stepElapsedTime = 0;
                remainingTime = 0;
            }
            else if (step.TryMatch(out TweenStep tweenStep))
            {
                stepElapsedTime += remainingTime;
                if (stepElapsedTime >= tweenStep.Duration)
                {
                    ExecuteCommand(tweenStep.Do, entityJsonNode, BuildTweenContext(entityJsonNode, 1.0f));
                    remainingTime = stepElapsedTime - tweenStep.Duration;
                    currentStepIndex++;
                    stepElapsedTime = 0;
                }
                else
                {
                    var t = stepElapsedTime / tweenStep.Duration;
                    // senior-dev: Linear interpolation only (no easing functions yet)
                    var interpolatedValue = tweenStep.From + (tweenStep.To - tweenStep.From) * t;
                    ExecuteCommand(tweenStep.Do, entityJsonNode, BuildTweenContext(entityJsonNode, interpolatedValue));
                    remainingTime = 0;
                }
            }
            else if (step.TryMatch(out RepeatStep repeatStep))
            {
                stepElapsedTime += remainingTime;

                var repeatContext = BuildRepeatContext(entityJsonNode, stepElapsedTime);
                if (TryEvaluateCondition(repeatStep.Until, repeatContext, out var conditionMet) && conditionMet)
                {
                    // senior-dev: Don't carry over time from repeat step to avoid unexpected behavior
                    remainingTime = 0;
                    currentStepIndex++;
                    stepElapsedTime = 0;
                }
                else
                {
                    // senior-dev: Execute command for each interval that passed this frame
                    // If multiple intervals passed, execute the command multiple times
                    // carrying forward mutations from each execution
                    var intervalsPassed = (int)(stepElapsedTime / repeatStep.Every);
                    var lastExecutedInterval = (int)((stepElapsedTime - remainingTime) / repeatStep.Every);
                    
                    for (var i = lastExecutedInterval; i < intervalsPassed; i++)
                    {
                        var execTime = (i + 1) * repeatStep.Every;
                        var execContext = BuildRepeatContext(entityJsonNode, execTime);
                        ExecuteCommand(repeatStep.Do, entityJsonNode, execContext);
                    }

                    remainingTime = 0;
                }
            }
            else
            {
                // senior-dev: Unknown step type - skip it and push error
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Unknown sequence step type at step {currentStepIndex} in sequence {sequenceIndex}"
                ));
                currentStepIndex++;
                stepElapsedTime = 0;
            }

            state = new SequenceState(currentStepIndex, stepElapsedTime);
        }

        if (currentStepIndex >= sequence.Steps.Length)
        {
            return true; // Sequence complete, should be removed
        }
        
        // Update sequence state
        sequenceStates.Set(sequenceIndex, state);
        return false; // Sequence still running
    }

    /// <summary>
    /// Build context for DoStep execution.
    /// Reuses pre-allocated context object.
    /// </summary>
    private JsonObject BuildDoContext(JsonNode entityJsonNode, float deltaTime)
    {
        _doContext["world"]!["deltaTime"] = deltaTime;
        _doContext["self"] = entityJsonNode;
        return _doContext;
    }

    /// <summary>
    /// Build context for TweenStep execution.
    /// Reuses pre-allocated context object.
    /// </summary>
    private JsonObject BuildTweenContext(JsonNode entityJsonNode, float tweenValue)
    {
        _tweenContext["tween"] = tweenValue;
        _tweenContext["self"] = entityJsonNode;
        return _tweenContext;
    }

    /// <summary>
    /// Build context for RepeatStep execution.
    /// Reuses pre-allocated context object.
    /// </summary>
    private JsonObject BuildRepeatContext(JsonNode entityJsonNode, float elapsed)
    {
        _repeatContext["elapsed"] = elapsed;
        _repeatContext["self"] = entityJsonNode;
        return _repeatContext;
    }

    /// <summary>
    /// Execute a SceneCommand with the given context.
    /// Only MutCommand is supported within sequences (no recursive sequence commands).
    /// </summary>
    private static void ExecuteCommand(SceneCommand command, JsonNode entityJsonNode, JsonObject context)
    {
        if (command.TryMatch(out MutCommand mutCommand))
        {
            mutCommand.Execute(entityJsonNode, context);
        }
    }

    /// <summary>
    /// Evaluate a JsonLogic condition and return the boolean result.
    /// </summary>
    private static bool TryEvaluateCondition(
        JsonNode condition,
        JsonNode context,
        [NotNullWhen(true)]
        out bool result
    )
    {
        try
        {
            var evalResult = JsonLogic.Apply(condition, context);
            if (evalResult != null && evalResult is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out var boolValue))
            {
                result = boolValue;
                return true;
            }

            result = false;
            return false;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Repeat condition evaluation failed: {ex.Message}"
            ));
            result = false;
            return false;
        }
    }

    /// <summary>
    /// Start a sequence on an entity.
    /// </summary>
    public void StartSequence(ushort entityIndex, ushort sequenceIndex)
    {
        if (!_activeSequences.TryGetValue(entityIndex, out var sequenceStates))
        {
            // senior-dev: CRITICAL - Size must be MaxSequences (512) not MaxActiveSequencesPerEntity (8)
            // because we index by global sequence index
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

        if (!sequenceStates.Any())
        {
            _activeSequences.Remove(entityIndex);
        }
    }

    /// <summary>
    /// Reset a running sequence on an entity back to step 0.
    /// </summary>
    public void ResetSequence(ushort entityIndex, ushort sequenceIndex)
    {
        if (!_activeSequences.TryGetValue(entityIndex, out var sequenceStates))
        {
            return;
        }

        if (!sequenceStates.HasValue(sequenceIndex))
        {
            return;
        }

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
