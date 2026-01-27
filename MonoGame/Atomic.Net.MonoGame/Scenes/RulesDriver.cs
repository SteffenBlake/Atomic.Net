using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Tags;
using Atomic.Net.MonoGame.Transform;
using Json.Logic;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Frame execution engine for rules-based entity mutations.
/// Evaluates WHERE/DO clauses using JsonLogic and applies mutations to matched entities.
/// </summary>
public class RulesDriver : IEventHandler<InitializeEvent>, IEventHandler<ShutdownEvent>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
        EventBus<InitializeEvent>.Register(Instance);
        EventBus<ShutdownEvent>.Register(Instance);
    }

    public static RulesDriver Instance { get; private set; } = null!;

    // senior-dev: Pre-allocated buffers for entity serialization (reused each frame, zero allocations)
    private readonly List<JsonRuleEntity> _entityBuffer = new(Constants.MaxEntities);
    private readonly HashSet<ushort> _rulesToRemove = new(Constants.MaxRules);

    /// <summary>
    /// Executes all active rules for a single frame.
    /// Processes global and scene rules in SparseArray order.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds.</param>
    public void RunFrame(float deltaTime)
    {
        _rulesToRemove.Clear();

        // senior-dev: Iterate all rules in RuleRegistry.Rules SparseArray order
        for (ushort i = 0; i < Constants.MaxRules; i++)
        {
            if (!RuleRegistry.Instance.Rules.HasValue(i))
            {
                continue;
            }

            var rule = RuleRegistry.Instance.Rules[i];

            // senior-dev: Execute rule with error handling (never throw)
            if (!TryExecuteRule(rule, deltaTime, i))
            {
                _rulesToRemove.Add(i);
            }
        }

        // senior-dev: Remove rules that had errors to prevent error spam in future frames
        foreach (var ruleIndex in _rulesToRemove)
        {
            RuleRegistry.Instance.Rules.Remove(ruleIndex);
        }
    }

    private bool TryExecuteRule(JsonRule rule, float deltaTime, ushort ruleIndex)
    {
        // senior-dev: Step 1 - Get matched entities from selector
        var matches = rule.From.Matches;

        // senior-dev: Step 2 - Build World context with deltaTime and matched entities
        var worldContext = BuildWorldContext(deltaTime, matches);
        if (worldContext == null)
        {
            return true; // senior-dev: No entities matched, not an error
        }

            // senior-dev: Step 3 - Evaluate WHERE clause (if present)
            JsonArray? entitiesToMutate = null;
            if (rule.Where != null)
            {
                JsonNode? whereResult;
                try
                {
                    whereResult = JsonLogic.Apply(rule.Where, worldContext);
                }
                catch (Exception ex)
                {
                    EventBus<ErrorEvent>.Push(new ErrorEvent(
                        $"Rule {ruleIndex}: WHERE clause JsonLogic evaluation failed: {ex.Message}. Rule will be removed."
                    ));
                    return false;
                }
                
                // senior-dev: WHERE can return boolean or array
                if (whereResult is JsonValue boolValue && boolValue.TryGetValue<bool>(out var boolResult))
                {
                    if (!boolResult)
                    {
                        return true; // senior-dev: WHERE returned false, skip rule
                    }
                    // senior-dev: WHERE returned true, use all entities
                    entitiesToMutate = worldContext["entities"]?.AsArray();
                }
                else if (whereResult is JsonArray arrayResult)
                {
                    // senior-dev: WHERE returned filtered array
                    entitiesToMutate = arrayResult;
                }
                else
                {
                    EventBus<ErrorEvent>.Push(new ErrorEvent(
                        $"Rule {ruleIndex}: WHERE clause must return boolean or array, got {whereResult?.GetType().Name}. Rule will be removed."
                    ));
                    return false;
                }
            }
            else
            {
                // senior-dev: No WHERE clause, use all matched entities
                entitiesToMutate = worldContext["entities"]?.AsArray();
            }

            if (entitiesToMutate == null || entitiesToMutate.Count == 0)
            {
                return true; // senior-dev: No entities to mutate, not an error
            }

            // senior-dev: Step 4 - Build context for DO clause with filtered entities
            var doContext = new JsonObject
            {
                ["world"] = new JsonObject
                {
                    ["deltaTime"] = deltaTime
                },
                ["entities"] = entitiesToMutate
            };

            // senior-dev: Step 5 - Evaluate DO clause
            if (!rule.Do.TryMatch(out MutCommand mutCommand))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Rule {ruleIndex}: DO clause must be a MutCommand. Rule will be removed."
                ));
                return false;
            }

            JsonNode? doResult;
            try
            {
                doResult = JsonLogic.Apply(mutCommand.Mut, doContext);
            }
            catch (Exception ex)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Rule {ruleIndex}: DO clause JsonLogic evaluation failed: {ex.Message}. Rule will be removed."
                ));
                return false;
            }
            
            if (doResult is not JsonArray mutations)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Rule {ruleIndex}: DO clause must return array (use map operation), got {doResult?.GetType().Name}. Rule will be removed."
                ));
                return false;
            }

            // senior-dev: Step 6 - Apply mutations back to entities
            ApplyMutations(mutations, ruleIndex);

            return true;
    }

    /// <summary>
    /// Builds World context JsonNode for JsonLogic evaluation.
    /// </summary>
    private JsonObject? BuildWorldContext(float deltaTime, SparseArray<bool> matches)
    {
        // senior-dev: Clear and reuse entity buffer (zero allocations)
        _entityBuffer.Clear();

        // senior-dev: Iterate matches SparseArray and serialize matched entities
        for (ushort i = 0; i < Constants.MaxEntities; i++)
        {
            if (!matches.HasValue(i) || !matches[i])
            {
                continue;
            }

            var entity = EntityRegistry.Instance[i];
            if (!entity.Active)
            {
                continue;
            }

            var jsonEntity = SerializeEntity(entity);
            _entityBuffer.Add(jsonEntity);
        }

        if (_entityBuffer.Count == 0)
        {
            return null; // senior-dev: No matched entities
        }

        // senior-dev: Serialize to JsonNode for JsonLogic
        // Based on DISCOVERIES.md: Standard JsonSerializer is fastest, no pooling needed
        // IMPORTANT: We need to serialize fresh for each call to avoid "node already has a parent" error
        var entitiesJson = JsonSerializer.SerializeToNode(_entityBuffer);

        var worldContext = new JsonObject
        {
            ["world"] = new JsonObject
            {
                ["deltaTime"] = deltaTime
            },
            ["entities"] = entitiesJson
        };

        return worldContext;
    }

    /// <summary>
    /// Serializes an entity to JsonRuleEntity with _index and all behaviors.
    /// </summary>
    private JsonRuleEntity SerializeEntity(Entity entity)
    {
        var jsonEntity = new JsonRuleEntity
        {
            _index = entity.Index
        };

        // senior-dev: Extract id via EntityIdRegistry
        if (BehaviorRegistry<IdBehavior>.Instance.TryGetBehavior(entity, out var idBehavior))
        {
            jsonEntity.id = idBehavior.Value.Id;
        }

        // senior-dev: Extract tags via TagRegistry
        if (BehaviorRegistry<TagsBehavior>.Instance.TryGetBehavior(entity, out var tagsBehavior))
        {
            jsonEntity.tags = tagsBehavior.Value.Tags.ToArray();
        }

        // senior-dev: Extract transform
        if (BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity, out var transform))
        {
            jsonEntity.transform = transform;
        }

        // senior-dev: Extract world transform
        if (BehaviorRegistry<WorldTransformBehavior>.Instance.TryGetBehavior(entity, out var worldTransform))
        {
            jsonEntity.worldTransform = worldTransform;
        }

        // senior-dev: Extract properties via PropertiesRegistry
        if (BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity, out var properties))
        {
            jsonEntity.properties = properties;
        }

        return jsonEntity;
    }

    /// <summary>
    /// Applies mutation results back to entities via _index property.
    /// </summary>
    private void ApplyMutations(JsonArray mutations, ushort ruleIndex)
    {
        foreach (var mutationNode in mutations)
        {
            if (mutationNode == null)
            {
                continue;
            }

            if (mutationNode is not JsonObject mutation)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Rule {ruleIndex}: Mutation must be a JsonObject, got {mutationNode.GetType().Name}. Skipping mutation."
                ));
                continue;
            }

            // senior-dev: Extract _index to identify target entity
            if (!mutation.TryGetPropertyValue("_index", out var indexNode) || indexNode == null)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Rule {ruleIndex}: Mutation missing _index property. Skipping mutation."
                ));
                continue;
            }

            if (!indexNode.AsValue().TryGetValue<ushort>(out var entityIndex))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Rule {ruleIndex}: Mutation _index is not a valid ushort. Skipping mutation."
                ));
                continue;
            }

            // senior-dev: Validate _index bounds
            if (entityIndex >= Constants.MaxEntities)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Rule {ruleIndex}: Mutation _index {entityIndex} out of bounds. Skipping mutation."
                ));
                continue;
            }

            var entity = EntityRegistry.Instance[entityIndex];
            if (!entity.Active)
            {
                // senior-dev: Entity deactivated mid-frame, skip silently (not an error)
                continue;
            }

            // senior-dev: Apply property mutations
            if (mutation.TryGetPropertyValue("properties", out var propertiesNode))
            {
                ApplyPropertiesMutation(entity, propertiesNode, ruleIndex);
            }

            // senior-dev: Apply tag mutations (if present)
            if (mutation.TryGetPropertyValue("tags", out var tagsNode))
            {
                ApplyTagsMutation(entity, tagsNode, ruleIndex);
            }
        }
    }

    /// <summary>
    /// Applies properties mutations to an entity.
    /// </summary>
    private void ApplyPropertiesMutation(Entity entity, JsonNode? propertiesNode, ushort ruleIndex)
    {
        if (propertiesNode == null)
        {
            return;
        }

        try
        {
            // senior-dev: Get current properties or create new empty dict
            var currentProps = BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity, out var existingPropsBehavior) && existingPropsBehavior.Value.Properties != null
                ? existingPropsBehavior.Value.Properties
                : new Dictionary<string, PropertyValue>();

            // senior-dev: Create new dictionary preserving existing properties
            var newPropsDict = new Dictionary<string, PropertyValue>(currentProps);

            // senior-dev: Merge in new/updated properties from mutation
            var mutatedProps = propertiesNode.AsObject();
            foreach (var kvp in mutatedProps)
            {
                var key = kvp.Key;
                var valueNode = kvp.Value;

                if (valueNode == null)
                {
                    // senior-dev: Null value means remove property
                    newPropsDict.Remove(key);
                    continue;
                }

                // senior-dev: Convert JsonNode to PropertyValue
                PropertyValue propValue;
                if (valueNode is JsonValue jsonValue)
                {
                    if (jsonValue.TryGetValue<bool>(out var boolVal))
                    {
                        propValue = new PropertyValue(boolVal);
                    }
                    else if (jsonValue.TryGetValue<string>(out var stringVal))
                    {
                        propValue = new PropertyValue(stringVal);
                    }
                    else if (jsonValue.TryGetValue<float>(out var floatVal))
                    {
                        propValue = new PropertyValue(floatVal);
                    }
                    else if (jsonValue.TryGetValue<double>(out var doubleVal))
                    {
                        // senior-dev: JsonLogic returns decimals/doubles, convert to float
                        propValue = new PropertyValue((float)doubleVal);
                    }
                    else if (jsonValue.TryGetValue<int>(out var intVal))
                    {
                        // senior-dev: Convert int to float for PropertyValue
                        propValue = new PropertyValue((float)intVal);
                    }
                    else
                    {
                        EventBus<ErrorEvent>.Push(new ErrorEvent(
                            $"Rule {ruleIndex}: Unsupported property value type for '{key}'. Skipping property."
                        ));
                        continue;
                    }
                }
                else
                {
                    EventBus<ErrorEvent>.Push(new ErrorEvent(
                        $"Rule {ruleIndex}: Property value for '{key}' must be a primitive type. Skipping property."
                    ));
                    continue;
                }

                newPropsDict[key] = propValue;
            }

            // senior-dev: Update behavior with new properties (immutable update pattern)
            var newBehavior = new PropertiesBehavior(newPropsDict);
            entity.SetBehavior<PropertiesBehavior, PropertiesBehavior>(
                in newBehavior,
                (ref readonly input, ref behavior) => behavior = input
            );
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Rule {ruleIndex}: Failed to apply properties mutation: {ex.Message}"
            ));
        }
    }

    /// <summary>
    /// Applies tags mutations to an entity.
    /// </summary>
    private void ApplyTagsMutation(Entity entity, JsonNode? tagsNode, ushort ruleIndex)
    {
        if (tagsNode == null)
        {
            return;
        }

        try
        {
            if (tagsNode is not JsonArray tagsArray)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Rule {ruleIndex}: Tags must be an array. Skipping tags mutation."
                ));
                return;
            }

            var tags = new List<string>();
            foreach (var tagNode in tagsArray)
            {
                if (tagNode is JsonValue tagValue && tagValue.TryGetValue<string>(out var tag))
                {
                    tags.Add(tag);
                }
            }

            // senior-dev: Update behavior with new tags (immutable update pattern)
            var newBehavior = new TagsBehavior
            {
                Tags = tags.ToImmutableHashSet()
            };
            entity.SetBehavior<TagsBehavior, TagsBehavior>(
                in newBehavior,
                (ref readonly input, ref behavior) => behavior = input
            );
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Rule {ruleIndex}: Failed to apply tags mutation: {ex.Message}"
            ));
        }
    }

    public void OnEvent(InitializeEvent _)
    {
        // senior-dev: Buffers are pre-allocated in constructor, no additional init needed
    }

    public void OnEvent(ShutdownEvent _)
    {
        // senior-dev: Clear buffers for cleanup
        _entityBuffer.Clear();
        _rulesToRemove.Clear();
    }
}
