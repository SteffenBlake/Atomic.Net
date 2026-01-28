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
public sealed class RulesDriver : ISingleton<RulesDriver>, IEventHandler<ShutdownEvent>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = new();
        EventBus<ShutdownEvent>.Register(Instance);
    }

    public static RulesDriver Instance { get; private set; } = null!;

    public void OnEvent(ShutdownEvent _)
    {
        EventBus<ShutdownEvent>.Unregister(this);
    }

    // senior-dev: Pre-allocated buffers reused across frames
    private readonly JsonArray _entitiesArray = new();
    private readonly JsonObject _doContext = new();

    /// <summary>
    /// Executes all active rules for a single frame.
    /// Mutates entities based on WHERE filtering and DO mutation operations.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds</param>
    public void RunFrame(float deltaTime)
    {
        // senior-dev: Process all active rules using SparseArray iteration (only iterates active items)
        var rules = RuleRegistry.Instance.Rules;
        foreach (var (_, rule) in rules)
        {
            ProcessRule(rule, deltaTime);
        }
    }

    /// <summary>
    /// Processes a single rule: get selector matches, serialize those entities, evaluate WHERE, execute DO mutations.
    /// </summary>
    private void ProcessRule(JsonRule rule, float deltaTime)
    {
        // senior-dev: Get entities that match the FROM selector
        var selectorMatches = rule.From.Matches;
        
        // senior-dev: Serialize only the matched entities to JsonArray
        var entities = SerializeEntities(selectorMatches);
        
        // senior-dev: Build WHERE context with world data and entities
        var whereContext = new JsonObject
        {
            ["world"] = new JsonObject { ["deltaTime"] = deltaTime },
            ["entities"] = entities
        };

        // senior-dev: Evaluate WHERE clause to filter entities
        JsonNode? filteredResult;
        try
        {
            filteredResult = JsonLogic.Apply(rule.Where, whereContext);
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to evaluate WHERE clause: {ex.Message}"
            ));
            return; // Continue to next rule
        }

        // senior-dev: Validate WHERE returns JsonArray
        if (filteredResult is not JsonArray filteredEntities)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"WHERE clause did not return JsonArray (got: {filteredResult?.GetType().Name ?? "null"})"
            ));
            return; // Continue to next rule
        }

        // senior-dev: Extract mutations from DO command
        if (!TryGetMutations(rule.Do, out var mutations))
        {
            return; // Not a mutation command, skip
        }

        // senior-dev: Setup reusable DO context once per rule (following POC pattern)
        // CRITICAL: DeepClone to avoid "node already has a parent" errors
        _doContext["world"] = whereContext["world"]!.DeepClone();
        _doContext["entities"] = filteredEntities.DeepClone();

        // senior-dev: Process each filtered entity
        for (var i = 0; i < filteredEntities.Count; i++)
        {
            var entity = filteredEntities[i];
            if (entity == null)
            {
                continue;
            }

            // senior-dev: Update context (reuse, don't reallocate) - following POC pattern
            _doContext["self"] = entity.DeepClone();
            
            ProcessEntityMutations(entity, mutations, _doContext);
        }
    }

    /// <summary>
    /// Serializes entities that match the selector into a JsonArray.
    /// Reuses pre-allocated JsonArray buffer for zero allocations.
    /// </summary>
    private JsonArray SerializeEntities(SparseArray<bool> selectorMatches)
    {
        // senior-dev: Clear the array but keep the allocated capacity
        _entitiesArray.Clear();

        // senior-dev: Iterate only the entities that match the selector
        foreach (var (entityIndex, _) in selectorMatches)
        {
            var entity = EntityRegistry.Instance[entityIndex];
            // senior-dev: Create entity object with required fields
            var entityObj = new JsonObject
            {
                ["_index"] = entity.Index
            };

            // senior-dev: Add id if present
            if (BehaviorRegistry<IdBehavior>.Instance.TryGetBehavior(entity, out var idBehavior))
            {
                entityObj["id"] = idBehavior.Value.Id;
            }

            // senior-dev: Add tags if present
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

            // senior-dev: Add properties if present
            if (BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity, out var propertiesBehavior))
            {
                var propertiesObj = new JsonObject();
                if (propertiesBehavior.Value.Properties != null)
                {
                    foreach (var (key, value) in propertiesBehavior.Value.Properties)
                    {
                        // senior-dev: PropertyValue is a variant type, need to extract the actual value
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

            _entitiesArray.Add(entityObj);
        }

        return _entitiesArray;
    }

    /// <summary>
    /// Extracts mutation operations from a SceneCommand.
    /// </summary>
    private bool TryGetMutations(
        SceneCommand command,
        out MutOperation[] mutations
    )
    {
        mutations = [];

        // senior-dev: SceneCommand is a variant type, extract MutCommand
        var hasMut = command.TryMatch(out MutCommand mutCommand);
        if (!hasMut)
        {
            // senior-dev: This is not necessarily an error - rule might have other command types
            // For now, just skip silently as only mut commands are supported
            return false;
        }

        mutations = mutCommand.Operations;
        return true;
    }

    /// <summary>
    /// Processes all mutation operations for a single entity.
    /// </summary>
    private void ProcessEntityMutations(
        JsonNode entity,
        MutOperation[] mutations,
        JsonObject doContext
    )
    {
        // senior-dev: Apply each mutation operation using the pre-built context
        foreach (var operation in mutations)
        {
            // senior-dev: ApplyMutation handles errors internally, just continue on failure
            ApplyMutation(entity, operation, doContext);
        }
    }

    /// <summary>
    /// Applies a mutation operation to an entity.
    /// </summary>
    private bool ApplyMutation(JsonNode entityJson, MutOperation operation, JsonObject context)
    {
        // senior-dev: Extract entity index from JSON
        if (!TryGetEntityIndex(entityJson, out var entityIndex))
        {
            return false;
        }

        // senior-dev: Evaluate the value expression using JsonLogic
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

        // senior-dev: Parse target path and apply mutation
        return ApplyToTarget(entityIndex, operation.Target, computedValue);
    }

    /// <summary>
    /// Extracts the _index property from entity JSON.
    /// </summary>
    private bool TryGetEntityIndex(
        JsonNode entityJson,
        out ushort entityIndex
    )
    {
        entityIndex = 0;

        if (entityJson is not JsonObject entityObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Entity JSON is not a JsonObject"
            ));
            return false;
        }

        if (!entityObj.TryGetPropertyValue("_index", out var indexNode) || indexNode == null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Entity missing _index property"
            ));
            return false;
        }

        try
        {
            var indexValue = indexNode.GetValue<int>();
            if (indexValue < 0 || indexValue >= Constants.MaxEntities)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Entity _index {indexValue} out of bounds (max: {Constants.MaxEntities})"
                ));
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
            return false;
        }
    }

    /// <summary>
    /// Applies the computed value to the target path.
    /// Currently supports only properties mutations.
    /// </summary>
    private bool ApplyToTarget(ushort entityIndex, JsonNode target, JsonNode value)
    {
        if (target is not JsonObject targetObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Target is not a JsonObject for entity {entityIndex}"
            ));
            return false;
        }

        // senior-dev: Check if this is a properties mutation
        if (targetObj.TryGetPropertyValue("properties", out var propertyKeyNode) && propertyKeyNode != null)
        {
            return ApplyPropertyMutation(entityIndex, propertyKeyNode, value);
        }

        // senior-dev: Unsupported target path format
        EventBus<ErrorEvent>.Push(new ErrorEvent(
            $"Unsupported target path format for entity {entityIndex}. Only 'properties' is currently supported."
        ));
        return false;
    }

    /// <summary>
    /// Applies a property mutation to an entity.
    /// </summary>
    private bool ApplyPropertyMutation(ushort entityIndex, JsonNode propertyKeyNode, JsonNode value)
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

        // senior-dev: Convert JsonNode value to PropertyValue
        if (!TryConvertToPropertyValue(value, out var propertyValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to convert value to PropertyValue for entity {entityIndex}, property '{propertyKey}'"
            ));
            return false;
        }

        // senior-dev: Get or create entity
        var entity = EntityRegistry.Instance[entityIndex];
        
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Cannot mutate inactive entity {entityIndex}"
            ));
            return false;
        }

        // senior-dev: Get existing properties or create new dictionary
        var existingProperties = BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity, out var behavior)
            ? behavior.Value.Properties
            : null;

        var newProperties = existingProperties != null
            ? new Dictionary<string, PropertyValue>(existingProperties)
            : new Dictionary<string, PropertyValue>();
        
        newProperties[propertyKey] = propertyValue.Value;

        // senior-dev: Set the updated properties behavior
        BehaviorRegistry<PropertiesBehavior>.Instance.SetBehavior(
            entity,
            ref newProperties,
            static (ref readonly Dictionary<string, PropertyValue> props, ref PropertiesBehavior b) =>
            {
                b = new PropertiesBehavior(props);
            }
        );

        return true;
    }

    /// <summary>
    /// Converts a JsonNode to a PropertyValue.
    /// </summary>
    private bool TryConvertToPropertyValue(
        JsonNode value,
        [NotNullWhen(true)]
        out PropertyValue? propertyValue
    )
    {
        propertyValue = null;

        try
        {
            // senior-dev: Try each variant type in order
            if (value is JsonValue jsonValue)
            {
                // senior-dev: Try bool first (most specific)
                if (jsonValue.TryGetValue<bool>(out var boolVal))
                {
                    propertyValue = boolVal;
                    return true;
                }

                // senior-dev: Try float (covers decimal values from JsonLogic)
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
                    propertyValue = (float)intVal;
                    return true;
                }

                // senior-dev: Try string last (least specific)
                if (jsonValue.TryGetValue<string>(out var stringVal))
                {
                    propertyValue = stringVal;
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
