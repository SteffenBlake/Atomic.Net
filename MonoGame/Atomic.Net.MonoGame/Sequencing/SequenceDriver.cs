using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Scenes;
using Json.Logic;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Executes all active sequences per-frame.
/// Processes sequences entity-first to enable batched mutation application.
/// </summary>
public sealed class SequenceDriver : 
    ISingleton<SequenceDriver>,
    IEventHandler<PreEntityDeactivatedEvent>,
    IEventHandler<UpdateFrameEvent>
{
    private readonly SparseReferenceArray<SparseArray<SequenceState>> _activeSequences = 
        new(Constants.MaxEntities);
    
    // Pre-allocated context objects to avoid allocations in hot path (zero-alloc principle)
    private readonly JsonObject _stepContext = new();
    private readonly JsonObject _worldContext = new() { ["deltaTime"] = 0f };
    private readonly JsonObject _repeatContext = new();

    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = new();
        EventBus<PreEntityDeactivatedEvent>.Register(Instance);
        EventBus<UpdateFrameEvent>.Register(Instance);
    }

    public static SequenceDriver Instance { get; private set; } = null!;

    /// <summary>
    /// Handles update frame event by running all sequences.
    /// </summary>
    public void OnEvent(UpdateFrameEvent e)
    {
        RunFrame((float)e.Elapsed.TotalSeconds);
    }

    /// <summary>
    /// Executes all active sequences for a single frame.
    /// </summary>
    public void RunFrame(float deltaTime)
    {
        foreach (var (entityIndex, sequenceStates) in _activeSequences)
        {
            if (sequenceStates == null)
            {
                continue;
            }

            // Build entity JsonNode once for all sequences on this entity
            var entityJson = JsonEntityConverter.Read(entityIndex);

            // Process all sequences for this entity
            foreach (var (sequenceIndex, state) in sequenceStates)
            {
                if (!SequenceRegistry.Instance.TryResolveByIndex(sequenceIndex, out var sequence) || sequence == null)
                {
                    // Sequence was removed from registry, clean up
                    sequenceStates.Remove(sequenceIndex);
                    continue;
                }

                ProcessSequence(entityIndex, sequenceIndex, sequence.Value, state, deltaTime, entityJson);
            }

            // After processing all sequences for this entity, write changes back
            JsonEntityConverter.Write(entityJson, entityIndex);
        }
    }

    private void ProcessSequence(
        ushort entityIndex,
        ushort sequenceIndex,
        JsonSequence sequence,
        SequenceState state,
        float deltaTime,
        JsonNode entityJson
    )
    {
        var remainingTime = deltaTime;
        var currentStepIndex = state.StepIndex;
        var currentStepElapsed = state.StepElapsedTime;

        while (remainingTime > 0 && currentStepIndex < sequence.Steps.Length)
        {
            var step = sequence.Steps[currentStepIndex];
            var (completed, timeConsumed) = ProcessStep(
                step,
                entityIndex,
                entityJson,
                currentStepElapsed,
                remainingTime
            );

            currentStepElapsed += timeConsumed;
            remainingTime -= timeConsumed;

            if (completed)
            {
                // Move to next step, carry forward remaining time
                currentStepIndex++;
                currentStepElapsed = 0;
            }
            else
            {
                // Step not complete, stop processing
                break;
            }
        }

        // Update or remove sequence state
        var sequenceStates = _activeSequences[entityIndex];
        if (sequenceStates == null)
        {
            return;
        }

        if (currentStepIndex >= sequence.Steps.Length)
        {
            // Sequence completed, remove from active sequences
            sequenceStates.Remove(sequenceIndex);
            if (sequenceStates.Count == 0)
            {
                _activeSequences.Remove(entityIndex);
            }
        }
        else
        {
            // Update state
            sequenceStates.Set(sequenceIndex, new SequenceState(
                (byte)currentStepIndex,
                currentStepElapsed
            ));
        }
    }

    private (bool completed, float timeConsumed) ProcessStep(
        JsonSequenceStep step,
        ushort entityIndex,
        JsonNode entityJson,
        float stepElapsed,
        float deltaTime
    )
    {
        return step.Visit(
            delay => ProcessDelayStep(delay, stepElapsed, deltaTime),
            doStep => ProcessDoStep(doStep, entityIndex, entityJson, deltaTime),
            tween => ProcessTweenStep(tween, entityIndex, entityJson, stepElapsed, deltaTime),
            repeat => ProcessRepeatStep(repeat, entityIndex, entityJson, stepElapsed, deltaTime),
            () => (true, 0f) // Should never happen
        );
    }

    private (bool completed, float timeConsumed) ProcessDelayStep(
        DelayStep delay,
        float stepElapsed,
        float deltaTime
    )
    {
        var remainingDelay = delay.Duration - stepElapsed;
        if (deltaTime >= remainingDelay)
        {
            // Delay complete
            return (true, remainingDelay);
        }
        else
        {
            // Delay not complete
            return (false, deltaTime);
        }
    }

    private (bool completed, float timeConsumed) ProcessDoStep(
        DoStep doStep,
        ushort entityIndex,
        JsonNode entityJson,
        float deltaTime
    )
    {
        // Build context for do step (reuse pre-allocated context to avoid allocations)
        _worldContext["deltaTime"] = deltaTime;
        _stepContext["world"] = _worldContext;
        _stepContext["self"] = entityJson;  // entityJson is already isolated per entity, no clone needed

        // Execute command
        if (doStep.Do.TryMatch(out MutCommand mutCommand))
        {
            MutCmdDriver.Execute(mutCommand, _stepContext, entityJson);
        }

        // Do step completes immediately (advances on next frame)
        return (true, 0f);
    }

    private (bool completed, float timeConsumed) ProcessTweenStep(
        TweenStep tween,
        ushort entityIndex,
        JsonNode entityJson,
        float stepElapsed,
        float deltaTime
    )
    {
        var remainingDuration = tween.Duration - stepElapsed;
        var actualDelta = Math.Min(deltaTime, remainingDuration);
        
        // Calculate tween value for current frame
        var newElapsed = stepElapsed + actualDelta;
        var t = newElapsed / tween.Duration;
        var tweenValue = tween.From + (tween.To - tween.From) * t;

        // Build context for tween step (reuse pre-allocated context)
        _stepContext["tween"] = tweenValue;
        _stepContext["self"] = entityJson;  // entityJson is already isolated, no clone needed

        // Execute command
        if (tween.Do.TryMatch(out MutCommand mutCommand))
        {
            MutCmdDriver.Execute(mutCommand, _stepContext, entityJson);
        }

        var completed = newElapsed >= tween.Duration;
        return (completed, actualDelta);
    }

    private (bool completed, float timeConsumed) ProcessRepeatStep(
        RepeatStep repeat,
        ushort entityIndex,
        JsonNode entityJson,
        float stepElapsed,
        float deltaTime
    )
    {
        var newElapsed = stepElapsed + deltaTime;

        // Calculate how many triggers should occur in this frame
        var lastTrigger = (int)(stepElapsed / repeat.Every);
        var currentTrigger = (int)(newElapsed / repeat.Every);
        var triggerCount = currentTrigger - lastTrigger;

        // Execute command for each trigger (reuse context to avoid allocations)
        for (var i = 0; i < triggerCount; i++)
        {
            var triggerElapsed = (lastTrigger + i + 1) * repeat.Every;

            _stepContext["elapsed"] = triggerElapsed;
            _stepContext["self"] = entityJson;  // entityJson is already isolated, no clone needed

            // Execute command
            if (repeat.Do.TryMatch(out MutCommand mutCommand))
            {
                MutCmdDriver.Execute(mutCommand, _stepContext, entityJson);
            }
        }

        // Check if repeat condition is met (until clause)
        _repeatContext["elapsed"] = newElapsed;
        _repeatContext["self"] = entityJson;  // entityJson is already isolated, no clone needed

        var completed = false;
        try
        {
            // senior-dev: FINDING: We deserialize the Rule every frame here, which is suboptimal.
            // Ideally, we'd parse it once at sequence load time and cache it. However, that would
            // require changing the JsonSequence structure to store parsed Rules, which is a larger
            // refactor. For now, this is acceptable given repeat steps are typically short-lived.
            var untilRule = System.Text.Json.JsonSerializer.Deserialize<Rule>(repeat.Until.ToJsonString());
            if (untilRule != null)
            {
                var result = untilRule.Apply(_repeatContext);
                if (result != null && result is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out var boolResult))
                {
                    completed = boolResult;
                }
            }
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to evaluate repeat 'until' condition for entity {entityIndex}: {ex.Message}"
            ));
            completed = true; // Stop on error
        }

        return (completed, deltaTime);
    }

    /// <summary>
    /// Starts a sequence on an entity.
    /// </summary>
    public void StartSequence(ushort entityIndex, ushort sequenceIndex)
    {
        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Cannot start sequence on inactive entity {entityIndex}"
            ));
            return;
        }

        var sequenceStates = _activeSequences[entityIndex];
        if (sequenceStates == null)
        {
            sequenceStates = new SparseArray<SequenceState>(Constants.MaxActiveSequencesPerEntity);
            _activeSequences[entityIndex] = sequenceStates;
        }

        // Start sequence from beginning
        sequenceStates.Set(sequenceIndex, new SequenceState(0, 0f));
    }

    /// <summary>
    /// Stops a running sequence on an entity.
    /// </summary>
    public void StopSequence(ushort entityIndex, ushort sequenceIndex)
    {
        var sequenceStates = _activeSequences[entityIndex];
        if (sequenceStates == null)
        {
            return; // No-op if entity has no sequences
        }

        sequenceStates.Remove(sequenceIndex);

        if (sequenceStates.Count == 0)
        {
            _activeSequences.Remove(entityIndex);
        }
    }

    /// <summary>
    /// Resets a sequence on an entity, restarting it from the beginning.
    /// </summary>
    public void ResetSequence(ushort entityIndex, ushort sequenceIndex)
    {
        var sequenceStates = _activeSequences[entityIndex];
        if (sequenceStates == null)
        {
            return; // No-op if entity has no sequences
        }

        if (sequenceStates.HasValue(sequenceIndex))
        {
            // Reset to beginning
            sequenceStates.Set(sequenceIndex, new SequenceState(0, 0f));
        }
    }

    /// <summary>
    /// Handles entity deactivation by removing all sequences for that entity.
    /// </summary>
    public void OnEvent(PreEntityDeactivatedEvent e)
    {
        _activeSequences.Remove(e.Entity.Index);
    }
}
