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

            var entity = EntityRegistry.Instance[entityIndex];
            if (!entity.Active)
            {
                continue;
            }

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
        // Validate entity exists
        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            return;
        }

        var entitySequences = _activeSequences[entityIndex];
        if (entitySequences == null)
        {
            // Lazy initialization - create SparseArray when first sequence starts
            entitySequences = new SparseArray<SequenceState>(Constants.MaxActiveSequencesPerEntity);
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
        var entitySequences = _activeSequences[entityIndex];
        if (entitySequences == null)
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
        var entitySequences = _activeSequences[entityIndex];
        if (entitySequences == null)
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
        var entitySequences = _activeSequences[evt.Entity.Index];
        if (entitySequences == null)
        {
            return;
        }

        entitySequences.Clear();
    }

    /// <summary>
    /// Processes a single sequence on an entity.
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

        if (state.StepIndex >= sequence.Steps.Length)
        {
            // Sequence completed - auto-remove
            StopSequence(entityIndex, sequenceIndex);
            return;
        }

        var currentStep = sequence.Steps[state.StepIndex];
        var newElapsedTime = state.StepElapsedTime + deltaTime;

        // Process step based on type
        currentStep.Visit(
            delay => ProcessDelayStep(entityIndex, sequenceIndex, state, delay, newElapsedTime),
            doStep => ProcessDoStep(entityIndex, sequenceIndex, state, doStep, entityJson, deltaTime),
            tween => ProcessTweenStep(entityIndex, sequenceIndex, state, tween, entityJson, newElapsedTime),
            repeat => ProcessRepeatStep(entityIndex, sequenceIndex, state, repeat, entityJson, newElapsedTime),
            () => {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Unknown sequence step type for sequence {sequenceIndex} on entity {entityIndex}"
                ));
                StopSequence(entityIndex, sequenceIndex);
            }
        );
    }

    /// <summary>
    /// Processes a delay step.
    /// </summary>
    private void ProcessDelayStep(
        ushort entityIndex,
        ushort sequenceIndex,
        SequenceState state,
        DelayStep delay,
        float newElapsedTime
    )
    {
        if (newElapsedTime >= delay.Duration)
        {
            // Delay complete - advance to next step
            var nextState = new SequenceState((byte)(state.StepIndex + 1), 0f);
            _activeSequences[entityIndex]!.Set(sequenceIndex, nextState);
        }
        else
        {
            // Still waiting - update elapsed time
            var updatedState = new SequenceState(state.StepIndex, newElapsedTime);
            _activeSequences[entityIndex]!.Set(sequenceIndex, updatedState);
        }
    }

    /// <summary>
    /// Processes a do step.
    /// </summary>
    private void ProcessDoStep(
        ushort entityIndex,
        ushort sequenceIndex,
        SequenceState state,
        DoStep doStep,
        JsonObject entityJson,
        float deltaTime
    )
    {
        // Build context: { "world": { "deltaTime": 0.016667 }, "self": { ... } }
        var context = new JsonObject
        {
            ["world"] = new JsonObject { ["deltaTime"] = deltaTime },
            ["self"] = entityJson
        };

        // Execute command
        ExecuteSequenceCommand(doStep.Do, context, entityIndex);

        // Advance to next step immediately
        var nextState = new SequenceState((byte)(state.StepIndex + 1), 0f);
        _activeSequences[entityIndex]!.Set(sequenceIndex, nextState);
    }

    /// <summary>
    /// Processes a tween step.
    /// </summary>
    private void ProcessTweenStep(
        ushort entityIndex,
        ushort sequenceIndex,
        SequenceState state,
        TweenStep tween,
        JsonObject entityJson,
        float newElapsedTime
    )
    {
        if (newElapsedTime >= tween.Duration)
        {
            // Tween complete - execute at target value and advance
            var context = new JsonObject
            {
                ["tween"] = tween.To,
                ["self"] = entityJson
            };

            ExecuteSequenceCommand(tween.Do, context, entityIndex);

            var nextState = new SequenceState((byte)(state.StepIndex + 1), 0f);
            _activeSequences[entityIndex]!.Set(sequenceIndex, nextState);
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
            _activeSequences[entityIndex]!.Set(sequenceIndex, updatedState);
        }
    }

    /// <summary>
    /// Processes a repeat step.
    /// </summary>
    private void ProcessRepeatStep(
        ushort entityIndex,
        ushort sequenceIndex,
        SequenceState state,
        RepeatStep repeat,
        JsonObject entityJson,
        float newElapsedTime
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
            return;
        }

        bool conditionMet = false;
        if (conditionResult is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out var boolValue))
        {
            conditionMet = boolValue;
        }

        if (conditionMet)
        {
            // Condition met - advance to next step
            var nextState = new SequenceState((byte)(state.StepIndex + 1), 0f);
            _activeSequences[entityIndex]!.Set(sequenceIndex, nextState);
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
            _activeSequences[entityIndex]!.Set(sequenceIndex, updatedState);
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
