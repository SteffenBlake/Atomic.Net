using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Flex;
using Atomic.Net.MonoGame.Hierarchy;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Tags;
using Atomic.Net.MonoGame.Transform;
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

        JsonNode? filteredResult;
        try
        {
            filteredResult = JsonLogic.Apply(rule.Where, _worldContext);
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

        for (var i = 0; i < filteredEntities.Count; i++)
        {
            var entity = filteredEntities[i];
            // JsonLogic filter can return null entries in sparse results
            if (entity == null)
            {
                continue;
            }

            _worldContext["index"] = i;
            
            ProcessEntityMutations(entity, mutations, _worldContext);
        }
    }

    /// <summary>
    /// Serializes entities that match the selector into a JsonArray.
    /// Note: Array buffer is reused, but JsonObject instances are allocated per call.
    /// </summary>
    private void BuildEntitiesArray(SparseArray<bool> selectorMatches)
    {
        var entities = (JsonArray)_worldContext["entities"]!;
        entities.Clear();
        foreach (var (entityIndex, _) in selectorMatches)
        {
            var entity = EntityRegistry.Instance[entityIndex];
            var entityObj = new JsonObject
            {
                ["_index"] = entity.Index
            };

            // senior-dev: Serialize IdBehavior
            if (BehaviorRegistry<IdBehavior>.Instance.TryGetBehavior(entity, out var idBehavior))
            {
                entityObj["id"] = idBehavior.Value.Id;
            }

            // senior-dev: Serialize TagsBehavior
            if (BehaviorRegistry<TagsBehavior>.Instance.TryGetBehavior(entity, out var tagBehavior))
            {
                var tagsJson = new JsonArray();
                if (tagBehavior.Value.Tags != null)
                {
                    foreach (var tag in tagBehavior.Value.Tags)
                    {
                        tagsJson.Add(tag);
                    }
                }
                entityObj["tags"] = tagsJson;
            }

            // senior-dev: Serialize PropertiesBehavior
            if (BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity, out var propertiesBehavior))
            {
                var propertiesJson = new JsonObject();
                // senior-dev: No null check needed - ImmutableDictionary never returns null from getter
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
                        propertiesJson[key] = jsonValue;
                    }
                }
                entityObj["properties"] = propertiesJson;
            }

            // senior-dev: Serialize TransformBehavior as nested object
            if (BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity, out var transformBehavior))
            {
                var transformJson = new JsonObject
                {
                    ["position"] = SerializeVector3(transformBehavior.Value.Position),
                    ["rotation"] = SerializeQuaternion(transformBehavior.Value.Rotation),
                    ["scale"] = SerializeVector3(transformBehavior.Value.Scale),
                    ["anchor"] = SerializeVector3(transformBehavior.Value.Anchor)
                };
                entityObj["transform"] = transformJson;
            }

            // senior-dev: Serialize ParentBehavior as selector string
            if (BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(entity, out var parentBehavior))
            {
                // senior-dev: ParentSelector.ToString() returns the selector string
                entityObj["parent"] = parentBehavior.Value.ParentSelector.ToString();
            }

            // senior-dev: Serialize individual flex behaviors
            SerializeFlexBehaviors(entity, entityObj);

            entities.Add(entityObj);
        }
    }

    /// <summary>
    /// Serializes all individual flex behaviors for an entity.
    /// </summary>
    private static void SerializeFlexBehaviors(Entity entity, JsonObject entityObj)
    {
        if (BehaviorRegistry<FlexAlignItemsBehavior>.Instance.TryGetBehavior(entity, out var alignItems))
        {
            entityObj["flexAlignItems"] = alignItems.Value.Value.ToString();
        }

        if (BehaviorRegistry<FlexAlignSelfBehavior>.Instance.TryGetBehavior(entity, out var alignSelf))
        {
            entityObj["flexAlignSelf"] = alignSelf.Value.Value.ToString();
        }

        if (BehaviorRegistry<FlexBorderBottomBehavior>.Instance.TryGetBehavior(entity, out var borderBottom))
        {
            entityObj["flexBorderBottom"] = borderBottom.Value.Value;
        }

        if (BehaviorRegistry<FlexBorderLeftBehavior>.Instance.TryGetBehavior(entity, out var borderLeft))
        {
            entityObj["flexBorderLeft"] = borderLeft.Value.Value;
        }

        if (BehaviorRegistry<FlexBorderRightBehavior>.Instance.TryGetBehavior(entity, out var borderRight))
        {
            entityObj["flexBorderRight"] = borderRight.Value.Value;
        }

        if (BehaviorRegistry<FlexBorderTopBehavior>.Instance.TryGetBehavior(entity, out var borderTop))
        {
            entityObj["flexBorderTop"] = borderTop.Value.Value;
        }

        if (BehaviorRegistry<FlexDirectionBehavior>.Instance.TryGetBehavior(entity, out var direction))
        {
            entityObj["flexDirection"] = direction.Value.Value.ToString();
        }

        if (BehaviorRegistry<FlexGrowBehavior>.Instance.TryGetBehavior(entity, out var grow))
        {
            entityObj["flexGrow"] = grow.Value.Value;
        }

        if (BehaviorRegistry<FlexWrapBehavior>.Instance.TryGetBehavior(entity, out var wrap))
        {
            entityObj["flexWrap"] = wrap.Value.Value.ToString();
        }

        if (BehaviorRegistry<FlexZOverride>.Instance.TryGetBehavior(entity, out var zOverride))
        {
            entityObj["flexZOverride"] = zOverride.Value.ZIndex;
        }

        if (BehaviorRegistry<FlexHeightBehavior>.Instance.TryGetBehavior(entity, out var height))
        {
            entityObj["flexHeight"] = height.Value.Value;
            entityObj["flexHeightPercent"] = height.Value.Percent;
        }

        if (BehaviorRegistry<FlexJustifyContentBehavior>.Instance.TryGetBehavior(entity, out var justifyContent))
        {
            entityObj["flexJustifyContent"] = justifyContent.Value.Value.ToString();
        }

        if (BehaviorRegistry<FlexMarginBottomBehavior>.Instance.TryGetBehavior(entity, out var marginBottom))
        {
            entityObj["flexMarginBottom"] = marginBottom.Value.Value;
        }

        if (BehaviorRegistry<FlexMarginLeftBehavior>.Instance.TryGetBehavior(entity, out var marginLeft))
        {
            entityObj["flexMarginLeft"] = marginLeft.Value.Value;
        }

        if (BehaviorRegistry<FlexMarginRightBehavior>.Instance.TryGetBehavior(entity, out var marginRight))
        {
            entityObj["flexMarginRight"] = marginRight.Value.Value;
        }

        if (BehaviorRegistry<FlexMarginTopBehavior>.Instance.TryGetBehavior(entity, out var marginTop))
        {
            entityObj["flexMarginTop"] = marginTop.Value.Value;
        }

        if (BehaviorRegistry<FlexPaddingBottomBehavior>.Instance.TryGetBehavior(entity, out var paddingBottom))
        {
            entityObj["flexPaddingBottom"] = paddingBottom.Value.Value;
        }

        if (BehaviorRegistry<FlexPaddingLeftBehavior>.Instance.TryGetBehavior(entity, out var paddingLeft))
        {
            entityObj["flexPaddingLeft"] = paddingLeft.Value.Value;
        }

        if (BehaviorRegistry<FlexPaddingRightBehavior>.Instance.TryGetBehavior(entity, out var paddingRight))
        {
            entityObj["flexPaddingRight"] = paddingRight.Value.Value;
        }

        if (BehaviorRegistry<FlexPaddingTopBehavior>.Instance.TryGetBehavior(entity, out var paddingTop))
        {
            entityObj["flexPaddingTop"] = paddingTop.Value.Value;
        }

        if (BehaviorRegistry<FlexPositionBottomBehavior>.Instance.TryGetBehavior(entity, out var positionBottom))
        {
            entityObj["flexPositionBottom"] = positionBottom.Value.Value;
            entityObj["flexPositionBottomPercent"] = positionBottom.Value.Percent;
        }

        if (BehaviorRegistry<FlexPositionLeftBehavior>.Instance.TryGetBehavior(entity, out var positionLeft))
        {
            entityObj["flexPositionLeft"] = positionLeft.Value.Value;
            entityObj["flexPositionLeftPercent"] = positionLeft.Value.Percent;
        }

        if (BehaviorRegistry<FlexPositionRightBehavior>.Instance.TryGetBehavior(entity, out var positionRight))
        {
            entityObj["flexPositionRight"] = positionRight.Value.Value;
            entityObj["flexPositionRightPercent"] = positionRight.Value.Percent;
        }

        if (BehaviorRegistry<FlexPositionTopBehavior>.Instance.TryGetBehavior(entity, out var positionTop))
        {
            entityObj["flexPositionTop"] = positionTop.Value.Value;
            entityObj["flexPositionTopPercent"] = positionTop.Value.Percent;
        }

        if (BehaviorRegistry<FlexPositionTypeBehavior>.Instance.TryGetBehavior(entity, out var positionType))
        {
            entityObj["flexPositionType"] = positionType.Value.Value.ToString();
        }

        if (BehaviorRegistry<FlexWidthBehavior>.Instance.TryGetBehavior(entity, out var width))
        {
            entityObj["flexWidth"] = width.Value.Value;
            entityObj["flexWidthPercent"] = width.Value.Percent;
        }
    }

    /// <summary>
    /// Serializes a Vector3 to JsonObject with x, y, z fields.
    /// </summary>
    private static JsonObject SerializeVector3(Vector3 vector)
    {
        return new JsonObject
        {
            ["x"] = vector.X,
            ["y"] = vector.Y,
            ["z"] = vector.Z
        };
    }

    /// <summary>
    /// Serializes a Quaternion to JsonObject with x, y, z, w fields.
    /// </summary>
    private static JsonObject SerializeQuaternion(Quaternion quaternion)
    {
        return new JsonObject
        {
            ["x"] = quaternion.X,
            ["y"] = quaternion.Y,
            ["z"] = quaternion.Z,
            ["w"] = quaternion.W
        };
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
            $"Unsupported target path format for entity {entityIndex}. Expected one of: [properties]"
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

        // Wrap in a tuple to pass into the anti-closure overload
        var setter = (propertyKey, propertyValue.Value);
        // senior-dev: Use With to add/update property in FluentDictionary
        entity.SetBehavior<PropertiesBehavior, (string Key, PropertyValue Value)>(
            ref setter,
            (ref readonly _setter, ref b) => b = b with { 
                Properties = b.Properties.With(_setter.Key, setter.Value) 
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
