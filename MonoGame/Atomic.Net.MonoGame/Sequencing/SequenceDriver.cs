using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Scenes;
using Json.Logic;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Singleton that processes all active sequences per-frame.
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

    // Outer index: entity, Inner index: sequence
    private readonly SparseReferenceArray<SparseArray<SequenceState>> _activeSequences = new(Constants.MaxEntities);

    private SequenceDriver()
    {
        // Don't pre-allocate - create inner arrays lazily when sequences start
    }

    /// <summary>
    /// Main entry point - processes all active sequences for a single frame.
    /// </summary>
    public void RunFrame(float deltaTime)
    {
        // Iterate entity-first for efficient cleanup and batched mutations
        // Use SparseReferenceArray enumerator to only process entities with active sequences
        foreach (var (entityIndex, entitySequences) in _activeSequences)
        {
            if (entitySequences == null)
            {
                continue;
            }

            // Check if entity is active before accessing it
            if (!EntityRegistry.Instance.IsActive(new Entity(entityIndex)))
            {
                // Entity was deactivated - cleanup sequences
                entitySequences.Clear();
                continue;
            }

            var entity = EntityRegistry.Instance[entityIndex];

            // Build entity JSON once for all sequences on this entity
            var entityJson = EntityJsonHelper.BuildEntityJsonNode(entity);

            // Process all sequences for this entity
            foreach (var (sequenceIndex, state) in entitySequences)
            {
                ProcessSequence(entityIndex, sequenceIndex, state, entityJson, deltaTime);
            }
        }
    }

    /// <summary>
    /// Starts a sequence on an entity.
    /// </summary>
    public void StartSequence(ushort entityIndex, ushort sequenceIndex)
    {
        // Validate entity is active before accessing
        if (!EntityRegistry.Instance.IsActive(new Entity(entityIndex)))
        {
            return;
        }

        if (!_activeSequences.TryGetValue(entityIndex, out var entitySequences))
        {
            // Lazy initialization - create SparseArray when first sequence starts
            entitySequences = new SparseArray<SequenceState>(Constants.MaxSequences);
            _activeSequences[entityIndex] = entitySequences;
        }

        // Initialize sequence state at step 0 with 0 elapsed time
        var initialState = new SequenceState(0, 0f);
        entitySequences.Set(sequenceIndex, initialState);
    }

    /// <summary>
    /// Stops a sequence on an entity.
    /// </summary>
    public void StopSequence(ushort entityIndex, ushort sequenceIndex)
    {
        if (!_activeSequences.TryGetValue(entityIndex, out var entitySequences))
        {
            return;
        }

        entitySequences.Remove(sequenceIndex);
    }

    /// <summary>
    /// Resets a sequence on an entity (restart from beginning).
    /// </summary>
    public void ResetSequence(ushort entityIndex, ushort sequenceIndex)
    {
        if (!_activeSequences.TryGetValue(entityIndex, out var entitySequences))
        {
            return;
        }

        // Reset to step 0 with 0 elapsed time
        var resetState = new SequenceState(0, 0f);
        entitySequences.Set(sequenceIndex, resetState);
    }

    /// <summary>
    /// Handles entity deactivation by removing all sequences for that entity.
    /// </summary>
    public void OnEvent(PreEntityDeactivatedEvent evt)
    {
        if (!_activeSequences.TryGetValue(evt.Entity.Index, out var entitySequences))
        {
            return;
        }

        entitySequences.Clear();
    }

    /// <summary>
    /// Processes a single sequence on an entity.
    /// Loops through steps until one doesn't complete immediately, carrying over leftover time.
    /// </summary>
    private void ProcessSequence(
        ushort entityIndex,
        ushort sequenceIndex,
        SequenceState state,
        JsonObject entityJson,
        float deltaTime
    )
    {
        if (!SequenceRegistry.Instance.TryResolveByIndex(sequenceIndex, out var sequence))
        {
            // Sequence definition not found - remove from active
            StopSequence(entityIndex, sequenceIndex);
            return;
        }

        var currentState = state;
        var remainingTime = deltaTime;
        
        // Loop through steps, processing until one doesn't complete immediately
        while (currentState.StepIndex < sequence.Steps.Length)
        {
            var currentStep = sequence.Steps[currentState.StepIndex];
            var newElapsedTime = currentState.StepElapsedTime + remainingTime;
            
            // Track if step completed this iteration
            bool stepCompleted = false;
            byte nextStepIndex = currentState.StepIndex;
            float leftoverTime = 0f;
            
            // Process step based on type
            currentStep.Visit(
                delay => stepCompleted = ProcessDelayStep(entityIndex, sequenceIndex, currentState, delay, newElapsedTime, out nextStepIndex, out leftoverTime),
                doStep => stepCompleted = ProcessDoStep(entityIndex, sequenceIndex, currentState, doStep, entityJson, remainingTime, out nextStepIndex, out leftoverTime),
                tween => stepCompleted = ProcessTweenStep(entityIndex, sequenceIndex, currentState, tween, entityJson, newElapsedTime, out nextStepIndex, out leftoverTime),
                repeat => stepCompleted = ProcessRepeatStep(entityIndex, sequenceIndex, currentState, repeat, entityJson, newElapsedTime, out nextStepIndex, out leftoverTime),
                () => {
                    EventBus<ErrorEvent>.Push(new ErrorEvent(
                        $"Unknown sequence step type for sequence {sequenceIndex} on entity {entityIndex}"
                    ));
                    StopSequence(entityIndex, sequenceIndex);
                }
            );
            
            if (stepCompleted)
            {
                // Step completed - advance to next step with leftover time
                currentState = new SequenceState(nextStepIndex, 0f);
                remainingTime = leftoverTime;
            }
            else
            {
                // Step didn't complete - already saved state in Process* method
                break;
            }
        }
        
        // Check if sequence completed
        if (currentState.StepIndex >= sequence.Steps.Length)
        {
            // Sequence completed - auto-remove
            StopSequence(entityIndex, sequenceIndex);
        }
        else if (currentState != state)
        {
            // Step completed but sequence not done - save new state
            if (_activeSequences.TryGetValue(entityIndex, out var entitySequences))
            {
                entitySequences.Set(sequenceIndex, currentState);
            }
        }
    }

    /// <summary>
    /// Processes a delay step.
    /// Returns true if step completed, with leftover time.
    /// </summary>
    private bool ProcessDelayStep(
        ushort entityIndex,
        ushort sequenceIndex,
        SequenceState state,
        DelayStep delay,
        float newElapsedTime,
        out byte nextStepIndex,
        out float leftoverTime
    )
    {
        if (newElapsedTime >= delay.Duration)
        {
            // Delay complete - advance to next step with leftover time
            nextStepIndex = (byte)(state.StepIndex + 1);
            leftoverTime = newElapsedTime - delay.Duration;
            return true;
        }
        else
        {
            // Still waiting - update elapsed time
            var updatedState = new SequenceState(state.StepIndex, newElapsedTime);
            if (_activeSequences.TryGetValue(entityIndex, out var entitySequences))
            {
                entitySequences.Set(sequenceIndex, updatedState);
            }
            nextStepIndex = state.StepIndex;
            leftoverTime = 0f;
            return false;
        }
    }

    /// <summary>
    /// Processes a do step.
    /// Returns true (always completes immediately), carries forward all deltaTime.
    /// </summary>
    private bool ProcessDoStep(
        ushort entityIndex,
        ushort sequenceIndex,
        SequenceState state,
        DoStep doStep,
        JsonObject entityJson,
        float deltaTime,
        out byte nextStepIndex,
        out float leftoverTime
    )
    {
        // Build context: { "world": { "deltaTime": ... }, "self": { ... } }
        var context = new JsonObject
        {
            ["world"] = new JsonObject { ["deltaTime"] = deltaTime },
            ["self"] = entityJson
        };

        // Execute command
        ExecuteSequenceCommand(doStep.Do, context, entityIndex);

        // Advance to next step immediately, carry forward all deltaTime
        nextStepIndex = (byte)(state.StepIndex + 1);
        leftoverTime = deltaTime;
        return true;
    }

    /// <summary>
    /// Processes a tween step.
    /// Returns true if tween completed, with leftover time.
    /// </summary>
    private bool ProcessTweenStep(
        ushort entityIndex,
        ushort sequenceIndex,
        SequenceState state,
        TweenStep tween,
        JsonObject entityJson,
        float newElapsedTime,
        out byte nextStepIndex,
        out float leftoverTime
    )
    {
        if (newElapsedTime >= tween.Duration)
        {
            // Tween complete - execute at target value and advance with leftover time
            var context = new JsonObject
            {
                ["tween"] = tween.To,
                ["self"] = entityJson
            };

            ExecuteSequenceCommand(tween.Do, context, entityIndex);

            nextStepIndex = (byte)(state.StepIndex + 1);
            leftoverTime = newElapsedTime - tween.Duration;
            return true;
        }
        else
        {
            // Tween in progress - interpolate and execute
            var t = newElapsedTime / tween.Duration;
            var tweenValue = tween.From + (tween.To - tween.From) * t;

            var context = new JsonObject
            {
                ["tween"] = tweenValue,
                ["self"] = entityJson
            };

            ExecuteSequenceCommand(tween.Do, context, entityIndex);

            // Update elapsed time
            var updatedState = new SequenceState(state.StepIndex, newElapsedTime);
            if (_activeSequences.TryGetValue(entityIndex, out var entitySequences))
            {
                entitySequences.Set(sequenceIndex, updatedState);
            }
            nextStepIndex = state.StepIndex;
            leftoverTime = 0f;
            return false;
        }
    }

    /// <summary>
    /// Processes a repeat step.
    /// Returns true if condition met (consumes all time).
    /// </summary>
    private bool ProcessRepeatStep(
        ushort entityIndex,
        ushort sequenceIndex,
        SequenceState state,
        RepeatStep repeat,
        JsonObject entityJson,
        float newElapsedTime,
        out byte nextStepIndex,
        out float leftoverTime
    )
    {
        // Build context for condition evaluation
        var context = new JsonObject
        {
            ["elapsed"] = newElapsedTime,
            ["self"] = entityJson
        };

        // Check if condition is met
        JsonNode? conditionResult;
        try
        {
            conditionResult = JsonLogic.Apply(repeat.Until, context);
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to evaluate repeat condition for sequence {sequenceIndex} on entity {entityIndex}: {ex.Message}"
            ));
            StopSequence(entityIndex, sequenceIndex);
            nextStepIndex = state.StepIndex;
            leftoverTime = 0f;
            return false;
        }

        bool conditionMet = false;
        if (conditionResult is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out var boolValue))
        {
            conditionMet = boolValue;
        }

        if (conditionMet)
        {
            // Condition met - advance to next step (consumes all remaining time)
            nextStepIndex = (byte)(state.StepIndex + 1);
            leftoverTime = 0f;
            return true;
        }
        else
        {
            // Check if it's time to execute (using modulo for repeat timing)
            var shouldExecute = (int)(newElapsedTime / repeat.Every) > (int)(state.StepElapsedTime / repeat.Every);

            if (shouldExecute)
            {
                ExecuteSequenceCommand(repeat.Do, context, entityIndex);
            }

            // Update elapsed time
            var updatedState = new SequenceState(state.StepIndex, newElapsedTime);
            if (_activeSequences.TryGetValue(entityIndex, out var entitySequences))
            {
                entitySequences.Set(sequenceIndex, updatedState);
            }
            nextStepIndex = state.StepIndex;
            leftoverTime = 0f;
            return false;
        }
    }

    /// <summary>
    /// Executes a command within a sequence step.
    /// </summary>
    private static void ExecuteSequenceCommand(SceneCommand command, JsonObject context, ushort entityIndex)
    {
        // Handle MutCommand
        if (command.TryMatch(out MutCommand mutCommand))
        {
            MutCmdDriver.Execute(mutCommand, context, entityIndex);
            return;
        }

        // Handle SequenceStartCommand (sequence chaining)
        if (command.TryMatch(out SequenceStartCommand seqStartCmd))
        {
            SequenceStartCmdDriver.Execute(entityIndex, seqStartCmd.SequenceId);
            return;
        }

        // Handle SequenceStopCommand
        if (command.TryMatch(out SequenceStopCommand seqStopCmd))
        {
            SequenceStopCmdDriver.Execute(entityIndex, seqStopCmd.SequenceId);
            return;
        }

        // Handle SequenceResetCommand
        if (command.TryMatch(out SequenceResetCommand seqResetCmd))
        {
            SequenceResetCmdDriver.Execute(entityIndex, seqResetCmd.SequenceId);
            return;
        }

        EventBus<ErrorEvent>.Push(new ErrorEvent(
            $"Unrecognized command type in sequence for entity {entityIndex}"
        ));
    }
}
