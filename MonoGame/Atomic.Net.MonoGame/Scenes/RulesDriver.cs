using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Tags;
using Json.Logic;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Executes all active rules in a single frame.
/// Processes global and scene rules in SparseArray order.
/// </summary>
public sealed class RulesDriver : 
    ISingleton<RulesDriver>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = new();
    }

    public static RulesDriver Instance { get; private set; } = null!;

    private readonly JsonArray _entities = [];
    private readonly JsonObject _whereContext = [];
    private readonly JsonObject _doContext = [];

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
        PopulateEntitiesArray(selectorMatches);
        
        // CRITICAL: Use DeepClone to avoid "node already has a parent" errors when reusing contexts
        _whereContext["world"] = new JsonObject { ["deltaTime"] = deltaTime };
        _whereContext["entities"] = _entities.DeepClone();

        JsonNode? filteredResult;
        try
        {
            filteredResult = JsonLogic.Apply(rule.Where, _whereContext);
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to evaluate WHERE clause: {ex.Message}"
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

        if (!TryGetMutations(rule.Do, out var mutations))
        {
            return;
        }

        // CRITICAL: DeepClone to avoid "node already has a parent" errors
        _doContext["world"] = _whereContext["world"]!.DeepClone();
        _doContext["entities"] = filteredEntities.DeepClone();

        for (var i = 0; i < filteredEntities.Count; i++)
        {
            var entity = filteredEntities[i];
            if (entity == null)
            {
                continue;
            }

            _doContext["self"] = entity.DeepClone();
            
            ProcessEntityMutations(entity, mutations, _doContext);
        }
    }

    /// <summary>
    /// Serializes entities that match the selector into a JsonArray.
    /// Note: Array buffer is reused, but JsonObject instances are allocated per call.
    /// </summary>
    private void PopulateEntitiesArray(SparseArray<bool> selectorMatches)
    {
        _entities.Clear();

        foreach (var (entityIndex, _) in selectorMatches)
        {
            var entity = EntityRegistry.Instance[entityIndex];
            var entityObj = new JsonObject
            {
                ["_index"] = entity.Index
            };

            if (BehaviorRegistry<IdBehavior>.Instance.TryGetBehavior(entity, out var idBehavior))
            {
                entityObj["id"] = idBehavior.Value.Id;
            }

            if (BehaviorRegistry<TagsBehavior>.Instance.TryGetBehavior(entity, out var tagBehavior))
            {
                var tagsArray = new JsonArray();
                if (tagBehavior.Value.Tags != null)
                {
                    foreach (var tag in tagBehavior.Value.Tags)
                    {
                        tagsArray.Add(tag);
                    }
                }
                entityObj["tags"] = tagsArray;
            }

            if (BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity, out var propertiesBehavior))
            {
                var propertiesObj = new JsonObject();
                if (propertiesBehavior.Value.Properties != null)
                {
                    foreach (var (key, value) in propertiesBehavior.Value.Properties)
                    {
                        // PropertyValue is a variant type, extract actual value
                        var jsonValue = value.Visit(
                            s => (JsonNode?)JsonValue.Create(s),
                            f => JsonValue.Create(f),
                            b => JsonValue.Create(b),
                            () => null
                        );
                        
                        if (jsonValue != null)
                        {
                            propertiesObj[key] = jsonValue;
                        }
                    }
                }
                entityObj["properties"] = propertiesObj;
            }

            _entities.Add(entityObj);
        }
    }

    /// <summary>
    /// Extracts mutation operations from a SceneCommand.
    /// </summary>
    private static bool TryGetMutations(
        SceneCommand command,
        [NotNullWhen(true)]
        out MutOperation[]? mutations
    )
    {
        // SceneCommand is a variant type - extract MutCommand if present
        if (!command.TryMatch(out MutCommand mutCommand))
        {
            // Not an error - rule might have other command types
            mutations = null;
            return false;
        }

        mutations = mutCommand.Operations;
        return true;
    }

    /// <summary>
    /// Processes all mutation operations for a single entity.
    /// </summary>
    private static void ProcessEntityMutations(
        JsonNode entity,
        MutOperation[] mutations,
        JsonObject doContext
    )
    {
        foreach (var operation in mutations)
        {
            ApplyMutation(entity, operation, doContext);
        }
    }

    /// <summary>
    /// Applies a mutation operation to an entity.
    /// </summary>
    private static bool ApplyMutation(
        JsonNode entityJson, MutOperation operation, JsonObject context
    )
    {
        if (!TryGetEntityIndex(entityJson, out var entityIndex))
        {
            return false;
        }

        JsonNode? computedValue;
        try
        {
            computedValue = JsonLogic.Apply(operation.Value, context);
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to evaluate mutation value for entity {entityIndex}: {ex.Message}"
            ));
            return false;
        }

        if (computedValue == null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Mutation value evaluation returned null for entity {entityIndex}"
            ));
            return false;
        }

        return ApplyToTarget(entityIndex.Value, operation.Target, computedValue);
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
    /// Currently supports only properties mutations.
    /// </summary>
    private static bool ApplyToTarget(ushort entityIndex, JsonNode target, JsonNode value)
    {
        if (target is not JsonObject targetObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Target is not a JsonObject for entity {entityIndex}"
            ));
            return false;
        }

        if (targetObj.TryGetPropertyValue("properties", out var propertyKeyNode) && propertyKeyNode != null)
        {
            return ApplyPropertyMutation(entityIndex, propertyKeyNode, value);
        }

        EventBus<ErrorEvent>.Push(new ErrorEvent(
            $"Unsupported target path format for entity {entityIndex}. Only 'properties' is currently supported."
        ));
        return false;
    }

    /// <summary>
    /// Applies a property mutation to an entity.
    /// </summary>
    private static bool ApplyPropertyMutation(ushort entityIndex, JsonNode propertyKeyNode, JsonNode value)
    {
        string propertyKey;
        try
        {
            propertyKey = propertyKeyNode.GetValue<string>();
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse property key for entity {entityIndex}: {ex.Message}"
            ));
            return false;
        }

        if (!TryConvertToPropertyValue(value, out var propertyValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to convert value to PropertyValue for entity {entityIndex}, property '{propertyKey}'"
            ));
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Cannot mutate inactive entity {entityIndex}"
            ));
            return false;
        }

        var existingProperties = BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity, out var behavior)
            ? behavior.Value.Properties
            : null;

        var newProperties = existingProperties != null
            ? new Dictionary<string, PropertyValue>(existingProperties)
            : [];
        
        newProperties[propertyKey] = propertyValue.Value;

        entity.SetBehavior<PropertiesBehavior, Dictionary<string, PropertyValue>>(
            ref newProperties,
            static (ref readonly props, ref b) =>
            {
                b = new PropertiesBehavior(props);
            }
        );

        return true;
    }

    /// <summary>
    /// Converts a JsonNode to a PropertyValue.
    /// </summary>
    private static bool TryConvertToPropertyValue(
        JsonNode value,
        [NotNullWhen(true)]
        out PropertyValue? propertyValue
    )
    {
        try
        {
            // Try types in order: bool (most specific) → numeric → string (least specific)
            if (value is JsonValue jsonValue)
            {
                if (jsonValue.TryGetValue<bool>(out var boolVal))
                {
                    propertyValue = boolVal;
                    return true;
                }

                // JsonLogic returns decimal for arithmetic operations
                if (jsonValue.TryGetValue<decimal>(out var decimalVal))
                {
                    propertyValue = (float)decimalVal;
                    return true;
                }
                
                if (jsonValue.TryGetValue<float>(out var floatVal))
                {
                    propertyValue = floatVal;
                    return true;
                }
                
                if (jsonValue.TryGetValue<int>(out var intVal))
                {
                    propertyValue = intVal;
                    return true;
                }

                if (jsonValue.TryGetValue<string>(out var stringVal))
                {
                    propertyValue = stringVal;
                    return true;
                }
            }

            propertyValue = null;
            return false;
        }
        catch
        {
            propertyValue = null;
            return false;
        }
    }
}
