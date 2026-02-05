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

    // Track which partition each entity in the entities array belongs to (parallel to _ruleContext["entities"])
    // Pre-allocated array to prevent runtime allocations during gameplay
    private readonly bool[] _entityPartitions = new bool[Constants.MaxGlobalEntities + (int)Constants.MaxSceneEntities];
    private int _entityCount = 0;

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
        // Process global rules
        foreach (var (_, rule) in RuleRegistry.Instance.Rules.Global)
        {
            ProcessRule(rule, deltaTime);
        }

        // Process scene rules
        foreach (var (_, rule) in RuleRegistry.Instance.Rules.Scene)
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

            // Execute the scene command on this entity
            rule.Do.Execute(entityJsonNode, _ruleContext);

            // Write mutations back to real entity using partition tracked in C# code
            bool isGlobal = _entityPartitions[i];
            if (!entityJsonNode.TryGetEntityIndex(isGlobal, out var entityIndex))
            {
                return;
            }
            WriteEntityChanges(entityJsonNode, entityIndex.Value);
        }
    }

    /// <summary>
    /// Serializes entities that match the selector into a JsonArray.
    /// Partition metadata is tracked separately in _entityPartitions array (not in JSON).
    /// </summary>
    private void BuildEntitiesArray(PartitionedSparseArray<bool> selectorMatches)
    {
        var entities = (JsonArray)_ruleContext["entities"]!;
        entities.Clear();
        _entityCount = 0;

        // Add global entities
        foreach (var (entityIndex, _) in selectorMatches.Global)
        {
            var entity = EntityRegistry.Instance[(ushort)entityIndex];
            var jsonNode = JsonEntityConverter.Read(entity);
            entities.Add(jsonNode);
            _entityPartitions[_entityCount++] = true; // Track as global partition
        }

        // Add scene entities
        foreach (var (entityIndex, _) in selectorMatches.Scene)
        {
            var entity = EntityRegistry.Instance[(uint)entityIndex];
            var jsonNode = JsonEntityConverter.Read(entity);
            entities.Add(jsonNode);
            _entityPartitions[_entityCount++] = false; // Track as scene partition
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
    private static void WriteEntityChanges(JsonNode jsonEntity, PartitionIndex entityIndex)
    {
        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            return;
        }

        JsonEntityConverter.Write(jsonEntity, entity);
    }
}

