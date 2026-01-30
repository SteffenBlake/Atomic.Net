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
    private readonly SparseReferenceArray<SparseArray<SequenceState>> _activeSequences = 
        new(Constants.MaxEntities);

    /// <summary>
    /// Main entry point called per-frame to process all active sequences.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds</param>
    public void RunFrame(float deltaTime)
    {
        // Process each entity that has active sequences
        foreach (var (entityIndex, sequenceStates) in _activeSequences)
        {
            var entity = EntityRegistry.Instance[entityIndex];
            if (!entity.Active)
            {
                continue;
            }

            // Build entity JsonNode once for all sequences on this entity
            var entityJsonNode = JsonEntityConverter.Read(entity);

            // Process all sequences for this entity
            foreach (var (sequenceIndex, sequenceState) in sequenceStates)
            {
                if (!SequenceRegistry.Instance.TryResolveByIndex(sequenceIndex, out var sequence))
                {
                    // Sequence no longer exists, remove it
                    sequenceStates.Remove(sequenceIndex);
                    continue;
                }

                // Process this sequence with the given state and time
                ProcessSequence(
                    entityIndex,
                    sequenceIndex,
                    sequence,
                    sequenceState,
                    deltaTime,
                    entityJsonNode
                );
            }

            // Write all mutations back to the real entity once
            JsonEntityConverter.Write(entityJsonNode, entity);
        }
    }

    /// <summary>
    /// Process a single sequence instance, updating its state and executing steps.
    /// </summary>
    private void ProcessSequence(
        ushort entityIndex,
        ushort sequenceIndex,
        JsonSequence sequence,
        SequenceState state,
        float deltaTime,
        JsonNode entityJsonNode
    )
    {
        var currentStepIndex = state.StepIndex;
        var remainingTime = deltaTime;

        // Keep processing steps until we run out of time or complete the sequence
        while (currentStepIndex < sequence.Steps.Length && remainingTime > 0)
        {
            var step = sequence.Steps[currentStepIndex];
            var stepElapsedTime = state.StepElapsedTime;

            if (step.TryMatch(out DelayStep delayStep))
            {
                // Delay step: wait for duration
                stepElapsedTime += remainingTime;
                if (stepElapsedTime >= delayStep.Duration)
                {
                    // Delay complete, move to next step
                    remainingTime = stepElapsedTime - delayStep.Duration;
                    currentStepIndex++;
                    stepElapsedTime = 0;
                }
                else
                {
                    // Delay still running
                    remainingTime = 0;
                }
            }
            else if (step.TryMatch(out DoStep doStep))
            {
                // Do step: execute command once with deltaTime context
                var doContext = BuildDoContext(entityJsonNode, remainingTime);
                ExecuteCommand(doStep.Do, entityJsonNode, doContext);

                // Move to next step immediately (do steps execute once per frame)
                currentStepIndex++;
                stepElapsedTime = 0;
                remainingTime = 0; // Do step consumes the frame
            }
            else if (step.TryMatch(out TweenStep tweenStep))
            {
                // Tween step: interpolate and execute command per-frame
                stepElapsedTime += remainingTime;
                if (stepElapsedTime >= tweenStep.Duration)
                {
                    // Tween complete, execute final frame at t=1.0
                    var tweenContext = BuildTweenContext(entityJsonNode, 1.0f);
                    ExecuteCommand(tweenStep.Do, entityJsonNode, tweenContext);

                    // Move to next step
                    remainingTime = stepElapsedTime - tweenStep.Duration;
                    currentStepIndex++;
                    stepElapsedTime = 0;
                }
                else
                {
                    // Tween still running, execute for this frame
                    var t = stepElapsedTime / tweenStep.Duration;
                    var interpolatedValue = tweenStep.From + (tweenStep.To - tweenStep.From) * t;
                    var tweenContext = BuildTweenContext(entityJsonNode, interpolatedValue);
                    ExecuteCommand(tweenStep.Do, entityJsonNode, tweenContext);

                    remainingTime = 0;
                }
            }
            else if (step.TryMatch(out RepeatStep repeatStep))
            {
                // Repeat step: execute command every interval until condition met
                stepElapsedTime += remainingTime;

                // Check if condition is met
                var repeatContext = BuildRepeatContext(entityJsonNode, stepElapsedTime);
                if (TryEvaluateCondition(repeatStep.Until, repeatContext, out var conditionMet) && conditionMet)
                {
                    // Condition met, move to next step
                    remainingTime = 0; // Don't carry over time from repeat step
                    currentStepIndex++;
                    stepElapsedTime = 0;
                }
                else
                {
                    // Execute command for each interval that passed
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

            // Update state
            state = new SequenceState(currentStepIndex, stepElapsedTime);
        }

        // Update or remove sequence state
        if (currentStepIndex >= sequence.Steps.Length)
        {
            // Sequence complete, remove from active sequences
            var sequenceStates = _activeSequences[entityIndex];
            if (sequenceStates != null)
            {
                sequenceStates.Remove(sequenceIndex);

                // If no more sequences for this entity, remove the entity entry
                if (!sequenceStates.Any())
                {
                    _activeSequences.Remove(entityIndex);
                }
            }
        }
        else
        {
            // Update sequence state
            var sequenceStates = _activeSequences[entityIndex];
            if (sequenceStates != null)
            {
                sequenceStates.Set(sequenceIndex, state);
            }
        }
    }

    /// <summary>
    /// Build context for DoStep execution.
    /// </summary>
    private static JsonObject BuildDoContext(JsonNode entityJsonNode, float deltaTime)
    {
        return new JsonObject
        {
            ["world"] = new JsonObject
            {
                ["deltaTime"] = deltaTime
            },
            ["self"] = entityJsonNode.DeepClone()
        };
    }

    /// <summary>
    /// Build context for TweenStep execution.
    /// </summary>
    private static JsonObject BuildTweenContext(JsonNode entityJsonNode, float tweenValue)
    {
        return new JsonObject
        {
            ["tween"] = tweenValue,
            ["self"] = entityJsonNode.DeepClone()
        };
    }

    /// <summary>
    /// Build context for RepeatStep execution.
    /// </summary>
    private static JsonObject BuildRepeatContext(JsonNode entityJsonNode, float elapsed)
    {
        return new JsonObject
        {
            ["elapsed"] = elapsed,
            ["self"] = entityJsonNode.DeepClone()
        };
    }

    /// <summary>
    /// Execute a SceneCommand with the given context.
    /// </summary>
    private static void ExecuteCommand(SceneCommand command, JsonNode entityJsonNode, JsonObject context)
    {
        if (command.TryMatch(out MutCommand mutCommand))
        {
            mutCommand.Execute(entityJsonNode, context);
        }
        // Sequence commands within sequences are not supported (no recursive calls)
        // If needed in the future, add support here
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
        // Get or create the sequence states for this entity
        if (!_activeSequences.TryGetValue(entityIndex, out var sequenceStates))
        {
            sequenceStates = new SparseArray<SequenceState>(Constants.MaxActiveSequencesPerEntity);
            _activeSequences[entityIndex] = sequenceStates;
        }

        // Check if sequence is already running
        if (sequenceStates.HasValue(sequenceIndex))
        {
            // Sequence already running, do nothing (could restart if desired)
            return;
        }

        // Start the sequence at step 0
        sequenceStates.Set(sequenceIndex, new SequenceState(0, 0));
    }

    /// <summary>
    /// Stop a running sequence on an entity.
    /// </summary>
    public void StopSequence(ushort entityIndex, ushort sequenceIndex)
    {
        if (!_activeSequences.TryGetValue(entityIndex, out var sequenceStates))
        {
            return; // No sequences for this entity
        }

        sequenceStates.Remove(sequenceIndex);

        // If no more sequences for this entity, remove the entity entry
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
            return; // No sequences for this entity
        }

        if (!sequenceStates.HasValue(sequenceIndex))
        {
            return; // Sequence not running
        }

        // Reset to step 0
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
