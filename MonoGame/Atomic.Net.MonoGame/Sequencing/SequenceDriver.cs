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
    IEventHandler<PreEntityDeactivatedEvent>
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
    }

    public static SequenceDriver Instance { get; private set; } = null!;

    /// <summary>
    /// Executes all active sequences for a single frame.
    /// Called externally by the game loop (not via UpdateFrameEvent).
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
                if (!SequenceRegistry.Instance.Sequences.TryGetValue(sequenceIndex, out var sequence))
                {
                    // Sequence was removed from registry, clean up
                    sequenceStates.Remove(sequenceIndex);
                    continue;
                }

                ProcessSequence(entityIndex, sequenceIndex, sequence.Value, state, deltaTime, entityJson);
            }

            // After processing all sequences for this entity, write changes back
            JsonEntityConverter.Write(entityJson, EntityRegistry.Instance[entityIndex]);
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
        byte currentStepIndex = state.StepIndex;
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

            if (!completed)
            {
                // Step not complete, stop processing
                break;
            }

            // Move to next step, carry forward remaining time
            currentStepIndex++;
            currentStepElapsed = 0;
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
            return;
        }

        // Update state
        sequenceStates.Set(sequenceIndex, new SequenceState(
            currentStepIndex,
            currentStepElapsed
        ));
    }

    private (bool completed, float timeConsumed) ProcessStep(
        JsonSequenceStep step,
        ushort entityIndex,
        JsonNode entityJson,
        float stepElapsed,
        float deltaTime
    )
    {
        // Use TryMatch instead of Visit to avoid closure allocations in hot path
        // (Visit would capture variables and allocate closures every frame)
        if (step.TryMatch(out DelayStep delay))
        {
            return ProcessDelayStep(delay, stepElapsed, deltaTime);
        }
        
        if (step.TryMatch(out DoStep doStep))
        {
            return ProcessDoStep(doStep, entityIndex, entityJson, deltaTime);
        }
        
        if (step.TryMatch(out TweenStep tween))
        {
            return ProcessTweenStep(tween, entityIndex, entityJson, stepElapsed, deltaTime);
        }
        
        if (step.TryMatch(out RepeatStep repeat))
        {
            return ProcessRepeatStep(repeat, entityIndex, entityJson, stepElapsed, deltaTime);
        }

        // Should never happen - all step types handled above
        throw new InvalidOperationException($"Unknown sequence step type encountered for entity {entityIndex}");
    }

    private (bool completed, float timeConsumed) ProcessDelayStep(
        DelayStep delay,
        float stepElapsed,
        float deltaTime
    )
    {
        var remainingDelay = delay.Duration - stepElapsed;
        if (deltaTime < remainingDelay)
        {
            // Delay not complete
            return (false, deltaTime);
        }

        // Delay complete
        return (true, remainingDelay);
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

        // Execute command using shared SceneCommandDriver
        SceneCommandDriver.Execute(doStep.Do, _stepContext, entityJson, entityIndex);

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

        // Execute command using shared SceneCommandDriver
        SceneCommandDriver.Execute(tween.Do, _stepContext, entityJson, entityIndex);

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

            // Execute command using shared SceneCommandDriver
            SceneCommandDriver.Execute(repeat.Do, _stepContext, entityJson, entityIndex);
        }

        // Check if repeat condition is met (until clause)
        _repeatContext["elapsed"] = newElapsed;
        _repeatContext["self"] = entityJson;  // entityJson is already isolated, no clone needed

        var completed = false;
        try
        {
            var result = JsonLogic.Apply(repeat.Until, _repeatContext);
            if (result is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out var boolResult))
            {
                completed = boolResult;
            }
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to evaluate repeat 'until' condition: {ex.Message}"
            ));
            completed = true; // Stop on error
        }

        return (completed, deltaTime);
    }

    /// <summary>
    /// Starts a sequence on an entity.
    /// If the sequence is not already running, starts it from the beginning.
    /// </summary>
    public void StartSequence(ushort entityIndex, ushort sequenceIndex)
    {
        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Cannot start sequence on inactive entity"
            ));
            return;
        }

        var sequenceStates = _activeSequences[entityIndex];
        if (sequenceStates == null)
        {
            sequenceStates = new SparseArray<SequenceState>(8);
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
    /// Resets a sequence on an entity.
    /// If the sequence is not running, starts it.
    /// If it is running, restarts it from the beginning.
    /// </summary>
    public void ResetSequence(ushort entityIndex, ushort sequenceIndex)
    {
        var sequenceStates = _activeSequences[entityIndex];
        if (sequenceStates == null)
        {
            // Not running, so start it
            StartSequence(entityIndex, sequenceIndex);
            return;
        }

        // Reset to beginning (whether running or not)
        sequenceStates.Set(sequenceIndex, new SequenceState(0, 0f));
    }

    /// <summary>
    /// Handles entity deactivation by removing all sequences for that entity.
    /// </summary>
    public void OnEvent(PreEntityDeactivatedEvent e)
    {
        _activeSequences.Remove(e.Entity.Index);
    }
}
