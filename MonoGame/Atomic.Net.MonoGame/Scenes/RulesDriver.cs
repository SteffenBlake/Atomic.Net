using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Core.Extensions;
using Atomic.Net.MonoGame.Flex;
using Atomic.Net.MonoGame.Hierarchy;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Selectors;
using Atomic.Net.MonoGame.Tags;
using Atomic.Net.MonoGame.Transform;
using FlexLayoutSharp;
using Json.Logic;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Executes all active rules in a single frame.
/// Processes global and scene rules in SparseArray order.
/// </summary>
public sealed class RulesDriver : 
    ISingleton<RulesDriver>
{
    private readonly JsonObject _worldContext = new()
    {
        ["world"] = new JsonObject()
        {
            ["deltaTime"] = 0.0f
        },
        ["entities"] = new JsonArray()
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
        _worldContext["world"]!["deltaTime"] = deltaTime;
        _worldContext["index"] = -1;
        BuildEntitiesArray(selectorMatches);

        if (!TryApplyJsonLogic(rule.Where, _worldContext, out var filteredResult))
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
            var entity = filteredEntities[i];
            // JsonLogic filter can return null entries in sparse results
            if (entity == null)
            {
                continue;
            }

            // Update context for this entity
            _worldContext["index"] = i;
            // Remove and re-add self to avoid parent conflicts
            _worldContext.Remove("self");
            _worldContext["self"] = entity.DeepClone();
            
            // Execute the scene command on this entity
            rule.Do.Execute(entity, _worldContext);
            
            // Write mutations back to real entity
            if (TryGetEntityIndex(entity, out var entityIndex))
            {
                WriteEntityChanges(entity, entityIndex.Value);
            }
        }
    }

    /// <summary>
    /// Serializes entities that match the selector into a JsonArray.
    /// </summary>
    private void BuildEntitiesArray(SparseArray<bool> selectorMatches)
    {
        var entities = (JsonArray)_worldContext["entities"]!;
        entities.Clear();
        foreach (var (entityIndex, _) in selectorMatches)
        {
            var entity = EntityRegistry.Instance[entityIndex];
            entities.Add(JsonEntityConverter.Read(entity));
        }
    }

    /// <summary>
    /// Extracts the _index property from entity JSON.
    /// </summary>
    private static bool TryGetEntityIndex(
        JsonNode entityJson,
        [NotNullWhen(true)]
        out ushort? entityIndex
    )
    {
        if (entityJson is not JsonObject entityObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Entity JSON is not a JsonObject"
            ));
            entityIndex = null;
            return false;
        }

        if (!entityObj.TryGetPropertyValue("_index", out var indexNode) || indexNode == null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Entity missing _index property"
            ));
            entityIndex = null;
            return false;
        }

        try
        {
            // Entity index is ushort, but JsonLogic may serialize as int
            if (indexNode is JsonValue jsonValue && 
                jsonValue.TryGetValue<ushort>(out var ushortValue)
            )
            {
                entityIndex = ushortValue;
                return true;
            }

            var indexValue = indexNode.GetValue<int>();
            if (indexValue < 0 || indexValue >= Constants.MaxEntities)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Entity _index {indexValue} out of bounds (max: {Constants.MaxEntities})"
                ));
                entityIndex = null;
                return false;
            }

            entityIndex = (ushort)indexValue;
            return true;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse _index: {ex.Message}"
            ));
            entityIndex = null;
            return false;
        }
    }

    /// <summary>
    /// Applies the computed value to the target path.
    /// Target can be either:
    /// - A string for root-level scalar properties (e.g., "flexHeight", "parent", "id", "tags")
    /// - An object for nested properties (e.g., { "properties": "health" }, { "position": "x" })
    /// </summary>


    /// <summary>
    /// Applies a property mutation to an entity.
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
        catch
        {
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

