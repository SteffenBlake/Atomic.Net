using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Core.Extensions;
using Atomic.Net.MonoGame.Selectors;
using Atomic.Net.MonoGame.Sequencing;
using Json.Logic;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Executes all active rules in a single frame.
/// Processes global and scene rules in SparseArray order.
/// </summary>
public sealed class RulesDriver : 
    ISingleton<RulesDriver>
{
    private readonly JsonObject _ruleContext = new()
    {
        ["world"] = new JsonObject()
        {
            ["deltaTime"] = 0.0f
        },
        ["entities"] = new JsonArray(),
        ["index"] = -1
    };

    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = new();
    }

    public static RulesDriver Instance { get; private set; } = null!;

    /// <summary>
    /// Executes all active rules for a single frame.
    /// Mutates entities based on WHERE filtering and DO mutation operations.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds</param>
    public void RunFrame(float deltaTime)
    {
        foreach (var (_, rule) in RuleRegistry.Instance.Rules)
        {
            ProcessRule(rule, deltaTime);
        }
    }

    /// <summary>
    /// Processes a single rule: get selector matches, serialize those entities, evaluate WHERE, execute DO mutations.
    /// </summary>
    private void ProcessRule(JsonRule rule, float deltaTime)
    {
        var selectorMatches = rule.From.Matches;
        _ruleContext["world"]!["deltaTime"] = deltaTime;
        _ruleContext["index"] = -1;
        BuildEntitiesArray(selectorMatches);

        if (!TryApplyJsonLogic(rule.Where, _ruleContext, out var filteredResult))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Failed to evaluate WHERE clause"
            ));
            return;
        }

        if (filteredResult is not JsonArray filteredEntities)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"WHERE clause did not return JsonArray (got: {filteredResult?.GetType().Name ?? "null"})"
            ));
            return;
        }

        for (var i = 0; i < filteredEntities.Count; i++)
        {
            var entityJsonNode = filteredEntities[i];
            // JsonLogic filter can return null entries in sparse results
            if (entityJsonNode == null)
            {
                continue;
            }

            // Update context for this entity
            _ruleContext["index"] = i;

            if (!entityJsonNode.TryGetEntityIndex(out var entityIndex))
            {
                continue;
            }
            
            // Execute the scene command based on its type
            if (rule.Do.TryMatch(out MutCommand mutCommand))
            {
                // Execute mutation on JsonNode
                mutCommand.Execute(entityJsonNode, _ruleContext);
                
                // Write mutations back to real entity
                WriteEntityChanges(entityJsonNode, entityIndex.Value);
            }
            else if (rule.Do.TryMatch(out SequenceStartCommand startCommand))
            {
                SequenceStartCmdDriver.Instance.Execute(entityIndex.Value, startCommand.SequenceId);
            }
            else if (rule.Do.TryMatch(out SequenceStopCommand stopCommand))
            {
                SequenceStopCmdDriver.Instance.Execute(entityIndex.Value, stopCommand.SequenceId);
            }
            else if (rule.Do.TryMatch(out SequenceResetCommand resetCommand))
            {
                SequenceResetCmdDriver.Instance.Execute(entityIndex.Value, resetCommand.SequenceId);
            }
        }
    }

    /// <summary>
    /// Serializes entities that match the selector into a JsonArray.
    /// </summary>
    private void BuildEntitiesArray(SparseArray<bool> selectorMatches)
    {
        var entities = (JsonArray)_ruleContext["entities"]!;
        entities.Clear();
        foreach (var (entityIndex, _) in selectorMatches)
        {
            var entity = EntityRegistry.Instance[entityIndex];
            entities.Add(JsonEntityConverter.Read(entity));
        }
    }

    /// <summary>
    /// Applies JsonLogic evaluation to a rule with a given context.
    /// </summary>
    private static bool TryApplyJsonLogic(
        JsonNode rule,
        JsonNode context,
        [NotNullWhen(true)]
        out JsonNode? result
    )
    {
        try
        {
            var tempResult = JsonLogic.Apply(rule, context);
            if (tempResult == null)
            {
                result = null;
                return false;
            }
            result = tempResult;
            return true;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"JsonLogic evaluation failed: {ex.Message}"
            ));
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Writes changes from a mutated JsonNode entity back to the real Entity.
    /// </summary>
    private static void WriteEntityChanges(JsonNode jsonEntity, ushort entityIndex)
    {
        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            return;
        }

        JsonEntityConverter.Write(jsonEntity, entity);
    }
}

