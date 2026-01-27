using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Tags;
using Json.Logic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Collections.Immutable;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Executes all active rules in a single frame, mutating matched entities based on parsed logic.
/// Singleton service that processes both global and scene rules.
/// </summary>
public sealed class RulesDriver : 
    ISingleton<RulesDriver>,
    IEventHandler<InitializeEvent>,
    IEventHandler<ShutdownEvent>
{
    public static RulesDriver Instance { get; private set; } = null!;

    public static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
        EventBus<InitializeEvent>.Register(Instance);
        EventBus<ShutdownEvent>.Register(Instance);
    }

    // senior-dev: Pre-allocated buffers for zero-allocation frame execution
    private readonly List<JsonRuleEntity> _entityBuffer = new(Constants.MaxEntities);
    private readonly List<ushort> _rulesToRemove = new(Constants.MaxRules);
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public void OnEvent(InitializeEvent _)
    {
        // senior-dev: No initialization needed beyond constructor
    }

    public void OnEvent(ShutdownEvent _)
    {
        // senior-dev: Clear buffers on shutdown
        _entityBuffer.Clear();
        _rulesToRemove.Clear();
    }

    /// <summary>
    /// Execute all active rules for a single frame.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds.</param>
    public void RunFrame(float deltaTime)
    {
        _rulesToRemove.Clear();

        // senior-dev: Iterate all rules in RuleRegistry.Rules SparseArray
        for (ushort i = 0; i < Constants.MaxRules; i++)
        {
            if (!RuleRegistry.Instance.Rules.HasValue(i))
            {
                continue;
            }

            var rule = RuleRegistry.Instance.Rules[i];

            try
            {
                ProcessRule(rule, deltaTime, i);
            }
            catch (Exception ex)
            {
                // senior-dev: Fire ErrorEvent and mark rule for removal
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Rule {i} failed with exception: {ex.Message}. Rule will be removed to prevent future errors."
                ));
                _rulesToRemove.Add(i);
            }
        }

        // senior-dev: Remove failed rules to prevent error spam
        foreach (var ruleIndex in _rulesToRemove)
        {
            RuleRegistry.Instance.Rules.Remove(ruleIndex);
        }
    }

    private void ProcessRule(JsonRule rule, float deltaTime, ushort ruleIndex)
    {
        // senior-dev: Get matched entities from selector
        var matches = rule.From.Matches;

        // senior-dev: Build world context with deltaTime and matched entities
        var worldContext = BuildWorldContext(deltaTime, matches);

        // senior-dev: Evaluate WHERE clause if present
        JsonNode? filteredEntities = worldContext["entities"];
        if (rule.Where != null)
        {
            var whereResult = EvaluateWhere(rule.Where, worldContext);
            
            if (whereResult == null)
            {
                // senior-dev: WHERE returned null/undefined, skip rule
                return;
            }

            if (whereResult is JsonValue jsonValue)
            {
                // senior-dev: WHERE returned boolean
                if (jsonValue.TryGetValue<bool>(out var boolResult))
                {
                    if (!boolResult)
                    {
                        // senior-dev: WHERE returned false, skip rule
                        return;
                    }
                    // senior-dev: WHERE returned true, use original entities
                }
                else
                {
                    // senior-dev: WHERE returned non-boolean value
                    EventBus<ErrorEvent>.Push(new ErrorEvent(
                        $"Rule {ruleIndex}: WHERE clause returned non-boolean/non-array value. Rule will be removed."
                    ));
                    _rulesToRemove.Add(ruleIndex);
                    return;
                }
            }
            else if (whereResult is JsonArray)
            {
                // senior-dev: WHERE returned filtered array
                filteredEntities = whereResult;
            }
            else
            {
                // senior-dev: WHERE returned unexpected type
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Rule {ruleIndex}: WHERE clause returned unexpected type. Rule will be removed."
                ));
                _rulesToRemove.Add(ruleIndex);
                return;
            }
        }

        // senior-dev: Execute DO clause
        if (rule.Do.TryMatch(out MutCommand mutCommand))
        {
            var doContext = new JsonObject
            {
                ["world"] = worldContext["world"],
                ["entities"] = filteredEntities
            };

            var doResult = EvaluateDo(mutCommand.Mut, doContext);

            if (doResult is JsonArray mutationsArray)
            {
                ApplyMutations(mutationsArray, ruleIndex);
            }
            else
            {
                // senior-dev: DO clause did not return an array
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Rule {ruleIndex}: DO clause must return an array via map operation. Rule will be removed."
                ));
                _rulesToRemove.Add(ruleIndex);
            }
        }
    }

    private JsonObject BuildWorldContext(float deltaTime, SparseArray<bool> matches)
    {
        // senior-dev: Clear entity buffer and reuse for this frame
        _entityBuffer.Clear();

        // senior-dev: Iterate matches and serialize matched entities
        for (ushort i = 0; i < Constants.MaxEntities; i++)
        {
            if (!matches[i])
            {
                continue;
            }

            var entity = EntityRegistry.Instance[i];
            if (!entity.Active)
            {
                continue;
            }

            _entityBuffer.Add(JsonRuleEntity.FromEntity(entity));
        }

        // senior-dev: Serialize to JsonNode for JsonLogic
        var worldNode = new JsonObject
        {
            ["deltaTime"] = deltaTime
        };

        var entitiesNode = JsonSerializer.SerializeToNode(_entityBuffer, _jsonOptions);

        return new JsonObject
        {
            ["world"] = worldNode,
            ["entities"] = entitiesNode
        };
    }

    private JsonNode? EvaluateWhere(JsonNode where, JsonObject context)
    {
        // senior-dev: Use JsonLogic to evaluate WHERE clause
        var rule = JsonSerializer.Deserialize<Rule>(where);
        if (rule == null)
        {
            return null;
        }
        
        var result = rule.Apply(context);
        return result;
    }

    private JsonNode? EvaluateDo(JsonNode doClause, JsonObject context)
    {
        // senior-dev: Use JsonLogic to evaluate DO clause
        var rule = JsonSerializer.Deserialize<Rule>(doClause);
        if (rule == null)
        {
            return null;
        }
        
        var result = rule.Apply(context);
        return result;
    }

    private void ApplyMutations(JsonArray mutations, ushort ruleIndex)
    {
        // senior-dev: Apply each mutation to its corresponding entity
        foreach (var mutation in mutations)
        {
            if (mutation == null)
            {
                continue;
            }

            var mutationObj = mutation.AsObject();

            // senior-dev: Extract _index from mutation
            if (!mutationObj.TryGetPropertyValue("_index", out var indexNode))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Rule {ruleIndex}: Mutation missing _index property. Rule will be removed."
                ));
                _rulesToRemove.Add(ruleIndex);
                return;
            }

            if (indexNode?.AsValue().TryGetValue<ushort>(out var entityIndex) != true)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Rule {ruleIndex}: Mutation _index is not a valid ushort. Rule will be removed."
                ));
                _rulesToRemove.Add(ruleIndex);
                return;
            }

            // senior-dev: Validate entity index bounds
            if (entityIndex >= Constants.MaxEntities)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Rule {ruleIndex}: Mutation _index {entityIndex} out of bounds. Rule will be removed."
                ));
                _rulesToRemove.Add(ruleIndex);
                return;
            }

            var entity = EntityRegistry.Instance[entityIndex];
            if (!entity.Active)
            {
                // senior-dev: Entity was deactivated mid-frame, skip silently
                continue;
            }

            // senior-dev: Apply properties mutations
            if (mutationObj.TryGetPropertyValue("properties", out var propertiesNode) && propertiesNode != null)
            {
                ApplyPropertiesMutation(entity, propertiesNode.AsObject());
            }

            // senior-dev: Apply tags mutations if present
            if (mutationObj.TryGetPropertyValue("tags", out var tagsNode) && tagsNode != null)
            {
                ApplyTagsMutation(entity, tagsNode.AsArray());
            }
        }
    }

    private void ApplyPropertiesMutation(Entity entity, JsonObject propertiesObj)
    {
        // senior-dev: Get current properties or create empty
        var hasExisting = BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(
            entity, out var currentPropertiesRef
        );

        var newProperties = hasExisting && currentPropertiesRef.HasValue && currentPropertiesRef.Value.Properties != null
            ? new Dictionary<string, PropertyValue>(currentPropertiesRef.Value.Properties)
            : new Dictionary<string, PropertyValue>();

        // senior-dev: Merge mutation properties into current properties
        foreach (var kvp in propertiesObj)
        {
            var value = ConvertToPropertyValue(kvp.Value);
            newProperties[kvp.Key] = value;
        }

        // senior-dev: Update PropertiesBehavior with merged properties
        var newBehavior = new PropertiesBehavior(newProperties);
        entity.SetBehavior<PropertiesBehavior, PropertiesBehavior>(
            in newBehavior,
            (ref readonly input, ref behavior) => behavior = input
        );
    }

    private void ApplyTagsMutation(Entity entity, JsonArray tagsArray)
    {
        // senior-dev: Extract tags from JsonArray
        var tags = tagsArray
            .Where(t => t != null)
            .Select(t => t!.GetValue<string>())
            .ToImmutableHashSet();

        // senior-dev: Update TagsBehavior
        var newBehavior = new TagsBehavior { Tags = tags };
        entity.SetBehavior<TagsBehavior, TagsBehavior>(
            in newBehavior,
            (ref readonly input, ref behavior) => behavior = input
        );
    }

    private PropertyValue ConvertToPropertyValue(JsonNode? node)
    {
        if (node == null)
        {
            // senior-dev: Return default PropertyValue for null
            return default;
        }

        // senior-dev: Convert JsonNode to PropertyValue
        if (node is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<bool>(out var boolVal))
            {
                return boolVal;
            }
            if (jsonValue.TryGetValue<float>(out var floatVal))
            {
                return floatVal;
            }
            if (jsonValue.TryGetValue<double>(out var doubleVal))
            {
                return (float)doubleVal;
            }
            if (jsonValue.TryGetValue<long>(out var longVal))
            {
                return (float)longVal;
            }
            if (jsonValue.TryGetValue<string>(out var stringVal) && stringVal != null)
            {
                return stringVal;
            }
        }

        // senior-dev: For unsupported types, convert to string representation
        return node.ToJsonString();
    }
}
