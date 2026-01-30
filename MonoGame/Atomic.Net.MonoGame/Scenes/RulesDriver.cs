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
    /// 
    /// senior-dev: KNOWN PERFORMANCE ISSUE - This method allocates JsonObject instances per entity per frame.
    /// This is required for JsonLogic evaluation but violates zero-alloc principle.
    /// 
    /// TODO: Investigate alternatives:
    /// 1. Object pooling for JsonObject instances
    /// 2. Custom JsonLogic evaluator that works directly with entity data
    /// 3. Cache serialized entities and only update on mutation
    /// 
    /// Estimated allocation: ~1-2 KB per entity per frame (100 entities = 100-200 KB/frame at 60 FPS = 6-12 MB/s GC pressure)
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

            if (BehaviorRegistry<IdBehavior>.Instance.TryGetBehavior(entity, out var idBehavior))
            {
                entityObj["id"] = idBehavior.Value.Id;
            }

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

            if (BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity, out var propertiesBehavior))
            {
                var propertiesJson = new JsonObject();
                foreach (var (key, value) in propertiesBehavior.Value.Properties)
                {
                    var jsonValue = value.Visit(
                        static s => (JsonNode?)JsonValue.Create(s),
                        static f => JsonValue.Create(f),
                        static b => JsonValue.Create(b),
                        static () => null
                    );
                    
                    if (jsonValue != null)
                    {
                        propertiesJson[key] = jsonValue;
                    }
                }
                entityObj["properties"] = propertiesJson;
            }

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

            if (BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(entity, out var parentBehavior))
            {
                entityObj["parent"] = parentBehavior.Value.ParentSelector.ToString();
            }

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
    private void ProcessEntityMutations(
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
    private bool ApplyMutation(
        JsonNode entityJson, MutOperation operation, JsonObject context
    )
    {
        if (!TryGetEntityIndex(entityJson, out var entityIndex))
        {
            return false;
        }

        if (!TryApplyJsonLogic(operation.Value, context, out var computedValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to evaluate mutation value for entity {entityIndex}"
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
    /// Target can be either:
    /// - A string for root-level scalar properties (e.g., "flexHeight", "parent", "id", "tags")
    /// - An object for nested properties (e.g., { "properties": "health" }, { "position": "x" })
    /// </summary>
    private bool ApplyToTarget(ushort entityIndex, JsonNode target, JsonNode value)
    {
        // Build the target path recursively
        var path = new List<string>();
        if (!TryBuildTargetPath(target, path))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse target path for entity {entityIndex}"
            ));
            return false;
        }

        // Dispatch based on the path
        return ApplyTargetPath(entityIndex, path, value);
    }

    /// <summary>
    /// Recursively traverses the target JsonNode to build a list of property names.
    /// Base case: string value → add to path and return
    /// Recursive case: object → extract property name and recurse on value
    /// </summary>
    private static bool TryBuildTargetPath(JsonNode target, List<string> path)
    {
        // Base case: target is a string - this is a property name
        if (target is JsonValue targetValue && targetValue.TryGetValue<string>(out var propertyName))
        {
            path.Add(propertyName);
            return true;
        }

        // Recursive case: target is an object - navigate deeper
        if (target is not JsonObject targetObj)
        {
            return false;
        }

        // Get the first (and should be only) property
        var property = targetObj.FirstOrDefault();
        if (property.Key == null || property.Value == null)
        {
            return false;
        }

        // Add this level's property name
        path.Add(property.Key);

        // Recurse on the value
        return TryBuildTargetPath(property.Value, path);
    }

    /// <summary>
    /// Applies a mutation based on the parsed target path.
    /// Path examples:
    ///   ["flexWidth"] → direct flex property
    ///   ["properties", "health"] → entity property
    ///   ["position", "x"] → transform component
    /// </summary>
    private bool ApplyTargetPath(ushort entityIndex, List<string> path, JsonNode value)
    {
        if (path.Count == 0)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Target path is empty for entity {entityIndex}"
            ));
            return false;
        }

        // Single-level path: direct property mutation
        if (path.Count == 1)
        {
            return ApplyStringTarget(entityIndex, path[0], value);
        }

        // Multi-level path: dispatch based on root property
        var rootProperty = path[0];
        var leafProperty = path[path.Count - 1];

        return rootProperty switch
        {
            "properties" => ApplyPropertyMutation(entityIndex, JsonValue.Create(leafProperty), value),
            "position" => ApplyTransformVector3ComponentMutation(entityIndex, "position", JsonValue.Create(leafProperty), value),
            "rotation" => ApplyTransformQuaternionComponentMutation(entityIndex, JsonValue.Create(leafProperty), value),
            "scale" => ApplyTransformVector3ComponentMutation(entityIndex, "scale", JsonValue.Create(leafProperty), value),
            "anchor" => ApplyTransformVector3ComponentMutation(entityIndex, "anchor", JsonValue.Create(leafProperty), value),
            _ => HandleUnrecognizedRootProperty(entityIndex, rootProperty)
        };
    }

    /// <summary>
    /// Handles unrecognized root properties in target paths.
    /// </summary>
    private static bool HandleUnrecognizedRootProperty(ushort entityIndex, string rootProperty)
    {
        var validRootProperties = string.Join(", ", new[]
        {
            "properties", "position", "rotation", "scale", "anchor"
        });
        EventBus<ErrorEvent>.Push(new ErrorEvent(
            $"Unrecognized root property '{rootProperty}' in target path for entity {entityIndex}. Expected one of: {validRootProperties}"
        ));
        return false;
    }

    /// <summary>
    /// Applies a mutation for string-based targets (root-level scalar properties).
    /// </summary>
    private bool ApplyStringTarget(ushort entityIndex, string targetName, JsonNode value)
    {
        return targetName switch
        {
            "id" => ApplyIdMutation(entityIndex, value),
            "tags" => ApplyTagsMutation(entityIndex, value),
            "parent" => ApplyParentMutation(entityIndex, value),
            "flexAlignItems" => ApplyFlexAlignItemsMutation(entityIndex, value),
            "flexAlignSelf" => ApplyFlexAlignSelfMutation(entityIndex, value),
            "flexBorderBottom" => ApplyFlexBorderBottomMutation(entityIndex, value),
            "flexBorderLeft" => ApplyFlexBorderLeftMutation(entityIndex, value),
            "flexBorderRight" => ApplyFlexBorderRightMutation(entityIndex, value),
            "flexBorderTop" => ApplyFlexBorderTopMutation(entityIndex, value),
            "flexDirection" => ApplyFlexDirectionMutation(entityIndex, value),
            "flexGrow" => ApplyFlexGrowMutation(entityIndex, value),
            "flexWrap" => ApplyFlexWrapMutation(entityIndex, value),
            "flexZOverride" => ApplyFlexZOverrideMutation(entityIndex, value),
            "flexHeight" => ApplyFlexHeightValueMutation(entityIndex, value),
            "flexHeightPercent" => ApplyFlexHeightPercentMutation(entityIndex, value),
            "flexJustifyContent" => ApplyFlexJustifyContentMutation(entityIndex, value),
            "flexMarginBottom" => ApplyFlexMarginBottomMutation(entityIndex, value),
            "flexMarginLeft" => ApplyFlexMarginLeftMutation(entityIndex, value),
            "flexMarginRight" => ApplyFlexMarginRightMutation(entityIndex, value),
            "flexMarginTop" => ApplyFlexMarginTopMutation(entityIndex, value),
            "flexPaddingBottom" => ApplyFlexPaddingBottomMutation(entityIndex, value),
            "flexPaddingLeft" => ApplyFlexPaddingLeftMutation(entityIndex, value),
            "flexPaddingRight" => ApplyFlexPaddingRightMutation(entityIndex, value),
            "flexPaddingTop" => ApplyFlexPaddingTopMutation(entityIndex, value),
            "flexPositionBottom" => ApplyFlexPositionBottomValueMutation(entityIndex, value),
            "flexPositionBottomPercent" => ApplyFlexPositionBottomPercentMutation(entityIndex, value),
            "flexPositionLeft" => ApplyFlexPositionLeftValueMutation(entityIndex, value),
            "flexPositionLeftPercent" => ApplyFlexPositionLeftPercentMutation(entityIndex, value),
            "flexPositionRight" => ApplyFlexPositionRightValueMutation(entityIndex, value),
            "flexPositionRightPercent" => ApplyFlexPositionRightPercentMutation(entityIndex, value),
            "flexPositionTop" => ApplyFlexPositionTopValueMutation(entityIndex, value),
            "flexPositionTopPercent" => ApplyFlexPositionTopPercentMutation(entityIndex, value),
            "flexPositionType" => ApplyFlexPositionTypeMutation(entityIndex, value),
            "flexWidth" => ApplyFlexWidthValueMutation(entityIndex, value),
            "flexWidthPercent" => ApplyFlexWidthPercentMutation(entityIndex, value),
            
            // senior-dev: FINDING: Error path allocates string for error message.
            // This is acceptable because:
            // 1. Only occurs during scene load (not during gameplay)
            // 2. Indicates malformed JSON which should be fixed by designer
            // 3. Error path, not hot path - clarity > allocation in this case
            _ => HandleUnrecognizedStringTarget(entityIndex, targetName)
        };
    }

    /// <summary>
    /// Handles unrecognized string targets by logging an error with all valid options.
    /// </summary>
    private static bool HandleUnrecognizedStringTarget(ushort entityIndex, string targetName)
    {
        // NOTE: This allocates an array for the error message, but this is acceptable
        // because this is an error path that only occurs during scene load, not gameplay.
        var validStringTargets = string.Join(", ", new[]
        {
            "id", "tags", "parent",
            "flexAlignItems", "flexAlignSelf",
            "flexBorderBottom", "flexBorderLeft", "flexBorderRight", "flexBorderTop",
            "flexDirection", "flexGrow", "flexWrap", "flexZOverride",
            "flexHeight", "flexHeightPercent",
            "flexJustifyContent",
            "flexMarginBottom", "flexMarginLeft", "flexMarginRight", "flexMarginTop",
            "flexPaddingBottom", "flexPaddingLeft", "flexPaddingRight", "flexPaddingTop",
            "flexPositionBottom", "flexPositionBottomPercent",
            "flexPositionLeft", "flexPositionLeftPercent",
            "flexPositionRight", "flexPositionRightPercent",
            "flexPositionTop", "flexPositionTopPercent",
            "flexPositionType",
            "flexWidth", "flexWidthPercent"
        });
        EventBus<ErrorEvent>.Push(new ErrorEvent(
            $"Unrecognized string target '{targetName}' for entity {entityIndex}. Expected one of: {validStringTargets}"
        ));
        return false;
    }

    /// <summary>
    /// Applies a property mutation to an entity.
    /// </summary>
    private static bool ApplyPropertyMutation(ushort entityIndex, JsonNode propertyKeyNode, JsonNode value)
    {
        if (!propertyKeyNode.TryGetStringValue(out var propertyKey))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse property key for entity {entityIndex}"
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

        var setter = (propertyKey, propertyValue.Value);
        entity.SetBehavior<PropertiesBehavior, (string Key, PropertyValue Value)>(
            ref setter,
            static (ref readonly _setter, ref b) => b = b with { 
                Properties = b.Properties.With(_setter.Key, _setter.Value) 
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
        if (value is not JsonValue jsonValue)
        {
            propertyValue = null;
            return false;
        }

        if (jsonValue.TryGetValue<bool>(out var boolVal))
        {
            propertyValue = boolVal;
            return true;
        }

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

        propertyValue = null;
        return false;
    }

    /// <summary>
    /// Applies an Id mutation to an entity.
    /// </summary>
    private static bool ApplyIdMutation(ushort entityIndex, JsonNode value)
    {
        if (!value.TryGetStringValue(out var newId))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse id value for entity {entityIndex}. Id must be a string."
            ));
            return false;
        }

        if (string.IsNullOrWhiteSpace(newId))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Id value cannot be null, empty, or whitespace for entity {entityIndex}"
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

        entity.SetBehavior<IdBehavior, string>(
            in newId,
            static (ref readonly _newId, ref b) => b = b with { Id = _newId }
        );

        return true;
    }

    /// <summary>
    /// Applies a Tags mutation to an entity.
    /// Tags are set holistically from a JsonArray of strings.
    /// </summary>
    private bool ApplyTagsMutation(ushort entityIndex, JsonNode value)
    {
        if (value is not JsonArray tagsArray)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Tags value must be a JsonArray for entity {entityIndex}"
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

        // Clear existing tags first
        entity.SetBehavior<TagsBehavior>(
            static (ref b) => b = b with { Tags = b.Tags.Clear() }
        );

        for (var i = 0; i < tagsArray.Count; i++)
        {
            var tagNode = tagsArray[i];
            if (tagNode == null)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Tag at index {i} is null for entity {entityIndex}"
                ));
                return false;
            }

            if (!tagNode.TryGetStringValue(out var tag))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Failed to parse tag at index {i} for entity {entityIndex}. Tags must be strings."
                ));
                return false;
            }

            if (string.IsNullOrWhiteSpace(tag))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Tag at index {i} cannot be null, empty, or whitespace for entity {entityIndex}"
                ));
                return false;
            }

            entity.SetBehavior<TagsBehavior, string>(
                in tag,
                static (ref readonly _tag, ref b) => b = b with { Tags = b.Tags.With(_tag) }
            );
        }

        return true;
    }

    /// <summary>
    /// Applies a mutation to a Vector3 component (position, scale, or anchor) of TransformBehavior.
    /// Component parameter should be a string indicating which component to set: "x", "y", or "z".
    /// Value must be a scalar float.
    /// </summary>
    private static bool ApplyTransformVector3ComponentMutation(
        ushort entityIndex,
        string fieldName,
        JsonNode componentNode,
        JsonNode value
    )
    {
        if (!componentNode.TryGetStringValue(out var component))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Transform {fieldName} component must be a string (x, y, or z) for entity {entityIndex}"
            ));
            return false;
        }

        if (component != "x" && component != "y" && component != "z")
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Transform {fieldName} component must be 'x', 'y', or 'z', got '{component}' for entity {entityIndex}"
            ));
            return false;
        }

        if (!value.TryGetFloatValue(out var floatValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Transform {fieldName}.{component} value must be a number for entity {entityIndex}"
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

        if (!BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity, out var transform))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Entity {entityIndex} does not have TransformBehavior"
            ));
            return false;
        }

        // Get the current vector based on field name
        Vector3 currentVector = fieldName switch
        {
            "position" => transform.Value.Position,
            "scale" => transform.Value.Scale,
            "anchor" => transform.Value.Anchor,
            _ => throw new InvalidOperationException($"Unknown Vector3 field: {fieldName}")
        };

        // Update the specific component
        var newVector = component switch
        {
            "x" => currentVector with { X = floatValue },
            "y" => currentVector with { Y = floatValue },
            "z" => currentVector with { Z = floatValue },
            _ => throw new InvalidOperationException($"Unknown component: {component}")
        };

        // Apply the mutation based on field name
        switch (fieldName)
        {
            case "position":
                entity.SetBehavior<TransformBehavior, Vector3>(
                    in newVector,
                    static (ref readonly _vec, ref b) => b.Position = _vec
                );
                break;
            case "scale":
                entity.SetBehavior<TransformBehavior, Vector3>(
                    in newVector,
                    static (ref readonly _vec, ref b) => b.Scale = _vec
                );
                break;
            case "anchor":
                entity.SetBehavior<TransformBehavior, Vector3>(
                    in newVector,
                    static (ref readonly _vec, ref b) => b.Anchor = _vec
                );
                break;
        }

        return true;
    }

    /// <summary>
    /// Applies a mutation to a Quaternion component (rotation) of TransformBehavior.
    /// Component parameter should be a string indicating which component to set: "x", "y", "z", or "w".
    /// Value must be a scalar float.
    /// </summary>
    private static bool ApplyTransformQuaternionComponentMutation(
        ushort entityIndex,
        JsonNode componentNode,
        JsonNode value
    )
    {
        if (!componentNode.TryGetStringValue(out var component))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Transform rotation component must be a string (x, y, z, or w) for entity {entityIndex}"
            ));
            return false;
        }

        if (component != "x" && component != "y" && component != "z" && component != "w")
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Transform rotation component must be 'x', 'y', 'z', or 'w', got '{component}' for entity {entityIndex}"
            ));
            return false;
        }

        if (!value.TryGetFloatValue(out var floatValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Transform rotation.{component} value must be a number for entity {entityIndex}"
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

        if (!BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity, out var transform))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Entity {entityIndex} does not have TransformBehavior"
            ));
            return false;
        }

        var currentQuat = transform.Value.Rotation;
        var newQuat = component switch
        {
            "x" => currentQuat with { X = floatValue },
            "y" => currentQuat with { Y = floatValue },
            "z" => currentQuat with { Z = floatValue },
            "w" => currentQuat with { W = floatValue },
            _ => throw new InvalidOperationException($"Unknown component: {component}")
        };

        entity.SetBehavior<TransformBehavior, Quaternion>(
            in newQuat,
            static (ref readonly _quat, ref b) => b.Rotation = _quat
        );

        return true;
    }

    /// <summary>
    /// Applies a Parent mutation to an entity.
    /// </summary>
    private static bool ApplyParentMutation(ushort entityIndex, JsonNode value)
    {
        string selectorString;
        try
        {
            selectorString = value.GetValue<string>();
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse parent selector for entity {entityIndex}: {ex.Message}. Parent must be a selector string."
            ));
            return false;
        }

        if (string.IsNullOrWhiteSpace(selectorString))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Parent selector cannot be null, empty, or whitespace for entity {entityIndex}"
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

        if (!SelectorRegistry.Instance.TryParse(selectorString.AsSpan(), out var selector))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse parent selector '{selectorString}' for entity {entityIndex}"
            ));
            return false;
        }

        var parentBehavior = new ParentBehavior(selector);
        
        entity.SetBehavior<ParentBehavior, ParentBehavior>(
            in parentBehavior,
            static (ref readonly _parent, ref b) => b = _parent
        );

        return true;
    }

    // ========== Flex Enum Behaviors ==========

    /// <summary>
    /// Applies FlexAlignItems mutation.
    /// </summary>
    private static bool ApplyFlexAlignItemsMutation(ushort entityIndex, JsonNode value) =>
        ApplyEnumBehavior<FlexAlignItemsBehavior, Align>(
            entityIndex, 
            value, 
            "flexAlignItems",
            static v => new FlexAlignItemsBehavior(v)
        );

    /// <summary>
    /// Applies FlexAlignSelf mutation.
    /// </summary>
    private static bool ApplyFlexAlignSelfMutation(ushort entityIndex, JsonNode value) =>
        ApplyEnumBehavior<FlexAlignSelfBehavior, Align>(
            entityIndex, 
            value, 
            "flexAlignSelf",
            static v => new FlexAlignSelfBehavior(v)
        );

    /// <summary>
    /// Applies FlexDirection mutation.
    /// </summary>
    private static bool ApplyFlexDirectionMutation(ushort entityIndex, JsonNode value) =>
        ApplyEnumBehavior<FlexDirectionBehavior, FlexDirection>(
            entityIndex, 
            value, 
            "flexDirection",
            static v => new FlexDirectionBehavior(v)
        );

    /// <summary>
    /// Applies FlexWrap mutation.
    /// </summary>
    private static bool ApplyFlexWrapMutation(ushort entityIndex, JsonNode value) =>
        ApplyEnumBehavior<FlexWrapBehavior, Wrap>(
            entityIndex, 
            value, 
            "flexWrap",
            static v => new FlexWrapBehavior(v)
        );

    /// <summary>
    /// Applies FlexJustifyContent mutation.
    /// </summary>
    private static bool ApplyFlexJustifyContentMutation(ushort entityIndex, JsonNode value) =>
        ApplyEnumBehavior<FlexJustifyContentBehavior, Justify>(
            entityIndex, 
            value, 
            "flexJustifyContent",
            static v => new FlexJustifyContentBehavior(v)
        );

    /// <summary>
    /// Applies FlexPositionType mutation.
    /// </summary>
    private static bool ApplyFlexPositionTypeMutation(ushort entityIndex, JsonNode value) =>
        ApplyEnumBehavior<FlexPositionTypeBehavior, PositionType>(
            entityIndex, 
            value, 
            "flexPositionType",
            static v => new FlexPositionTypeBehavior(v)
        );

    // ========== Simple Float Behaviors ==========

    /// <summary>
    /// Applies FlexBorderBottom mutation.
    /// </summary>
    private static bool ApplyFlexBorderBottomMutation(ushort entityIndex, JsonNode value) =>
        ApplyFloatBehavior<FlexBorderBottomBehavior>(
            entityIndex, 
            value, 
            "flexBorderBottom",
            static v => new FlexBorderBottomBehavior(v)
        );

    /// <summary>
    /// Applies FlexBorderLeft mutation.
    /// </summary>
    private static bool ApplyFlexBorderLeftMutation(ushort entityIndex, JsonNode value) =>
        ApplyFloatBehavior<FlexBorderLeftBehavior>(
            entityIndex, 
            value, 
            "flexBorderLeft",
            static v => new FlexBorderLeftBehavior(v)
        );

    /// <summary>
    /// Applies FlexBorderRight mutation.
    /// </summary>
    private static bool ApplyFlexBorderRightMutation(ushort entityIndex, JsonNode value) =>
        ApplyFloatBehavior<FlexBorderRightBehavior>(
            entityIndex, 
            value, 
            "flexBorderRight",
            static v => new FlexBorderRightBehavior(v)
        );

    /// <summary>
    /// Applies FlexBorderTop mutation.
    /// </summary>
    private static bool ApplyFlexBorderTopMutation(ushort entityIndex, JsonNode value) =>
        ApplyFloatBehavior<FlexBorderTopBehavior>(
            entityIndex, 
            value, 
            "flexBorderTop",
            static v => new FlexBorderTopBehavior(v)
        );

    /// <summary>
    /// Applies FlexGrow mutation.
    /// </summary>
    private static bool ApplyFlexGrowMutation(ushort entityIndex, JsonNode value) =>
        ApplyFloatBehavior<FlexGrowBehavior>(
            entityIndex, 
            value, 
            "flexGrow",
            static v => new FlexGrowBehavior(v)
        );

    /// <summary>
    /// Applies FlexMarginBottom mutation.
    /// </summary>
    private static bool ApplyFlexMarginBottomMutation(ushort entityIndex, JsonNode value) =>
        ApplyFloatBehavior<FlexMarginBottomBehavior>(
            entityIndex, 
            value, 
            "flexMarginBottom",
            static v => new FlexMarginBottomBehavior(v)
        );

    /// <summary>
    /// Applies FlexMarginLeft mutation.
    /// </summary>
    private static bool ApplyFlexMarginLeftMutation(ushort entityIndex, JsonNode value) =>
        ApplyFloatBehavior<FlexMarginLeftBehavior>(
            entityIndex, 
            value, 
            "flexMarginLeft",
            static v => new FlexMarginLeftBehavior(v)
        );

    /// <summary>
    /// Applies FlexMarginRight mutation.
    /// </summary>
    private static bool ApplyFlexMarginRightMutation(ushort entityIndex, JsonNode value) =>
        ApplyFloatBehavior<FlexMarginRightBehavior>(
            entityIndex, 
            value, 
            "flexMarginRight",
            static v => new FlexMarginRightBehavior(v)
        );

    /// <summary>
    /// Applies FlexMarginTop mutation.
    /// </summary>
    private static bool ApplyFlexMarginTopMutation(ushort entityIndex, JsonNode value) =>
        ApplyFloatBehavior<FlexMarginTopBehavior>(
            entityIndex, 
            value, 
            "flexMarginTop",
            static v => new FlexMarginTopBehavior(v)
        );

    /// <summary>
    /// Applies FlexPaddingBottom mutation.
    /// </summary>
    private static bool ApplyFlexPaddingBottomMutation(ushort entityIndex, JsonNode value) =>
        ApplyFloatBehavior<FlexPaddingBottomBehavior>(
            entityIndex, 
            value, 
            "flexPaddingBottom",
            static v => new FlexPaddingBottomBehavior(v)
        );

    /// <summary>
    /// Applies FlexPaddingLeft mutation.
    /// </summary>
    private static bool ApplyFlexPaddingLeftMutation(ushort entityIndex, JsonNode value) =>
        ApplyFloatBehavior<FlexPaddingLeftBehavior>(
            entityIndex, 
            value, 
            "flexPaddingLeft",
            static v => new FlexPaddingLeftBehavior(v)
        );

    /// <summary>
    /// Applies FlexPaddingRight mutation.
    /// </summary>
    private static bool ApplyFlexPaddingRightMutation(ushort entityIndex, JsonNode value) =>
        ApplyFloatBehavior<FlexPaddingRightBehavior>(
            entityIndex, 
            value, 
            "flexPaddingRight",
            static v => new FlexPaddingRightBehavior(v)
        );

    /// <summary>
    /// Applies FlexPaddingTop mutation.
    /// </summary>
    private static bool ApplyFlexPaddingTopMutation(ushort entityIndex, JsonNode value) =>
        ApplyFloatBehavior<FlexPaddingTopBehavior>(
            entityIndex, 
            value, 
            "flexPaddingTop",
            static v => new FlexPaddingTopBehavior(v)
        );

    // ========== Two-Field Behaviors (Value + Percent) ==========

    /// <summary>
    /// Applies FlexHeight value mutation (not percent).
    /// </summary>
    private static bool ApplyFlexHeightValueMutation(ushort entityIndex, JsonNode value)
    {
        if (!value.TryGetFloatValue(out var floatValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"flexHeight value must be a number for entity {entityIndex}"
            ));
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        if (!BehaviorRegistry<FlexHeightBehavior>.Instance.TryGetBehavior(entity, out var currentBehavior))
        {
            // Create new behavior with default percent = false
            var newBehavior = new FlexHeightBehavior(floatValue, false);
            entity.SetBehavior<FlexHeightBehavior, FlexHeightBehavior>(
                in newBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        else
        {
            // Update existing behavior, keeping percent
            var updatedBehavior = new FlexHeightBehavior(floatValue, currentBehavior.Value.Percent);
            entity.SetBehavior<FlexHeightBehavior, FlexHeightBehavior>(
                in updatedBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        return true;
    }

    /// <summary>
    /// Applies FlexHeight percent mutation (not value).
    /// </summary>
    private static bool ApplyFlexHeightPercentMutation(ushort entityIndex, JsonNode value)
    {
        if (!value.TryGetBoolValue(out var boolValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"flexHeightPercent value must be a boolean for entity {entityIndex}"
            ));
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        if (!BehaviorRegistry<FlexHeightBehavior>.Instance.TryGetBehavior(entity, out var currentBehavior))
        {
            // Create new behavior with default value = 0
            var newBehavior = new FlexHeightBehavior(0, boolValue);
            entity.SetBehavior<FlexHeightBehavior, FlexHeightBehavior>(
                in newBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        else
        {
            // Update existing behavior, keeping value
            var updatedBehavior = new FlexHeightBehavior(currentBehavior.Value.Value, boolValue);
            entity.SetBehavior<FlexHeightBehavior, FlexHeightBehavior>(
                in updatedBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        return true;
    }

    /// <summary>
    /// Applies FlexWidth value mutation (not percent).
    /// </summary>
    private static bool ApplyFlexWidthValueMutation(ushort entityIndex, JsonNode value)
    {
        if (!value.TryGetFloatValue(out var floatValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"flexWidth value must be a number for entity {entityIndex}"
            ));
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        if (!BehaviorRegistry<FlexWidthBehavior>.Instance.TryGetBehavior(entity, out var currentBehavior))
        {
            var newBehavior = new FlexWidthBehavior(floatValue, false);
            entity.SetBehavior<FlexWidthBehavior, FlexWidthBehavior>(
                in newBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        else
        {
            var updatedBehavior = new FlexWidthBehavior(floatValue, currentBehavior.Value.Percent);
            entity.SetBehavior<FlexWidthBehavior, FlexWidthBehavior>(
                in updatedBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        return true;
    }

    /// <summary>
    /// Applies FlexWidth percent mutation (not value).
    /// </summary>
    private static bool ApplyFlexWidthPercentMutation(ushort entityIndex, JsonNode value)
    {
        if (!value.TryGetBoolValue(out var boolValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"flexWidthPercent value must be a boolean for entity {entityIndex}"
            ));
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        if (!BehaviorRegistry<FlexWidthBehavior>.Instance.TryGetBehavior(entity, out var currentBehavior))
        {
            var newBehavior = new FlexWidthBehavior(0, boolValue);
            entity.SetBehavior<FlexWidthBehavior, FlexWidthBehavior>(
                in newBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        else
        {
            var updatedBehavior = new FlexWidthBehavior(currentBehavior.Value.Value, boolValue);
            entity.SetBehavior<FlexWidthBehavior, FlexWidthBehavior>(
                in updatedBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        return true;
    }

    /// <summary>
    /// Applies FlexPositionBottom value mutation (not percent).
    /// </summary>
    private static bool ApplyFlexPositionBottomValueMutation(ushort entityIndex, JsonNode value)
    {
        if (!value.TryGetFloatValue(out var floatValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"flexPositionBottom value must be a number for entity {entityIndex}"
            ));
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        if (!BehaviorRegistry<FlexPositionBottomBehavior>.Instance.TryGetBehavior(entity, out var currentBehavior))
        {
            var newBehavior = new FlexPositionBottomBehavior(floatValue, false);
            entity.SetBehavior<FlexPositionBottomBehavior, FlexPositionBottomBehavior>(
                in newBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        else
        {
            var updatedBehavior = new FlexPositionBottomBehavior(floatValue, currentBehavior.Value.Percent);
            entity.SetBehavior<FlexPositionBottomBehavior, FlexPositionBottomBehavior>(
                in updatedBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        return true;
    }

    /// <summary>
    /// Applies FlexPositionBottom percent mutation (not value).
    /// </summary>
    private static bool ApplyFlexPositionBottomPercentMutation(ushort entityIndex, JsonNode value)
    {
        if (!value.TryGetBoolValue(out var boolValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"flexPositionBottomPercent value must be a boolean for entity {entityIndex}"
            ));
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        if (!BehaviorRegistry<FlexPositionBottomBehavior>.Instance.TryGetBehavior(entity, out var currentBehavior))
        {
            var newBehavior = new FlexPositionBottomBehavior(0, boolValue);
            entity.SetBehavior<FlexPositionBottomBehavior, FlexPositionBottomBehavior>(
                in newBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        else
        {
            var updatedBehavior = new FlexPositionBottomBehavior(currentBehavior.Value.Value, boolValue);
            entity.SetBehavior<FlexPositionBottomBehavior, FlexPositionBottomBehavior>(
                in updatedBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        return true;
    }

    /// <summary>
    /// Applies FlexPositionLeft value mutation (not percent).
    /// </summary>
    private static bool ApplyFlexPositionLeftValueMutation(ushort entityIndex, JsonNode value)
    {
        if (!value.TryGetFloatValue(out var floatValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"flexPositionLeft value must be a number for entity {entityIndex}"
            ));
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        if (!BehaviorRegistry<FlexPositionLeftBehavior>.Instance.TryGetBehavior(entity, out var currentBehavior))
        {
            var newBehavior = new FlexPositionLeftBehavior(floatValue, false);
            entity.SetBehavior<FlexPositionLeftBehavior, FlexPositionLeftBehavior>(
                in newBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        else
        {
            var updatedBehavior = new FlexPositionLeftBehavior(floatValue, currentBehavior.Value.Percent);
            entity.SetBehavior<FlexPositionLeftBehavior, FlexPositionLeftBehavior>(
                in updatedBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        return true;
    }

    /// <summary>
    /// Applies FlexPositionLeft percent mutation (not value).
    /// </summary>
    private static bool ApplyFlexPositionLeftPercentMutation(ushort entityIndex, JsonNode value)
    {
        if (!value.TryGetBoolValue(out var boolValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"flexPositionLeftPercent value must be a boolean for entity {entityIndex}"
            ));
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        if (!BehaviorRegistry<FlexPositionLeftBehavior>.Instance.TryGetBehavior(entity, out var currentBehavior))
        {
            var newBehavior = new FlexPositionLeftBehavior(0, boolValue);
            entity.SetBehavior<FlexPositionLeftBehavior, FlexPositionLeftBehavior>(
                in newBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        else
        {
            var updatedBehavior = new FlexPositionLeftBehavior(currentBehavior.Value.Value, boolValue);
            entity.SetBehavior<FlexPositionLeftBehavior, FlexPositionLeftBehavior>(
                in updatedBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        return true;
    }

    /// <summary>
    /// Applies FlexPositionRight value mutation (not percent).
    /// </summary>
    private static bool ApplyFlexPositionRightValueMutation(ushort entityIndex, JsonNode value)
    {
        if (!value.TryGetFloatValue(out var floatValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"flexPositionRight value must be a number for entity {entityIndex}"
            ));
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        if (!BehaviorRegistry<FlexPositionRightBehavior>.Instance.TryGetBehavior(entity, out var currentBehavior))
        {
            var newBehavior = new FlexPositionRightBehavior(floatValue, false);
            entity.SetBehavior<FlexPositionRightBehavior, FlexPositionRightBehavior>(
                in newBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        else
        {
            var updatedBehavior = new FlexPositionRightBehavior(floatValue, currentBehavior.Value.Percent);
            entity.SetBehavior<FlexPositionRightBehavior, FlexPositionRightBehavior>(
                in updatedBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        return true;
    }

    /// <summary>
    /// Applies FlexPositionRight percent mutation (not value).
    /// </summary>
    private static bool ApplyFlexPositionRightPercentMutation(ushort entityIndex, JsonNode value)
    {
        if (!value.TryGetBoolValue(out var boolValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"flexPositionRightPercent value must be a boolean for entity {entityIndex}"
            ));
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        if (!BehaviorRegistry<FlexPositionRightBehavior>.Instance.TryGetBehavior(entity, out var currentBehavior))
        {
            var newBehavior = new FlexPositionRightBehavior(0, boolValue);
            entity.SetBehavior<FlexPositionRightBehavior, FlexPositionRightBehavior>(
                in newBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        else
        {
            var updatedBehavior = new FlexPositionRightBehavior(currentBehavior.Value.Value, boolValue);
            entity.SetBehavior<FlexPositionRightBehavior, FlexPositionRightBehavior>(
                in updatedBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        return true;
    }

    /// <summary>
    /// Applies FlexPositionTop value mutation (not percent).
    /// </summary>
    private static bool ApplyFlexPositionTopValueMutation(ushort entityIndex, JsonNode value)
    {
        if (!value.TryGetFloatValue(out var floatValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"flexPositionTop value must be a number for entity {entityIndex}"
            ));
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        if (!BehaviorRegistry<FlexPositionTopBehavior>.Instance.TryGetBehavior(entity, out var currentBehavior))
        {
            var newBehavior = new FlexPositionTopBehavior(floatValue, false);
            entity.SetBehavior<FlexPositionTopBehavior, FlexPositionTopBehavior>(
                in newBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        else
        {
            var updatedBehavior = new FlexPositionTopBehavior(floatValue, currentBehavior.Value.Percent);
            entity.SetBehavior<FlexPositionTopBehavior, FlexPositionTopBehavior>(
                in updatedBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        return true;
    }

    /// <summary>
    /// Applies FlexPositionTop percent mutation (not value).
    /// </summary>
    private static bool ApplyFlexPositionTopPercentMutation(ushort entityIndex, JsonNode value)
    {
        if (!value.TryGetBoolValue(out var boolValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"flexPositionTopPercent value must be a boolean for entity {entityIndex}"
            ));
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        if (!BehaviorRegistry<FlexPositionTopBehavior>.Instance.TryGetBehavior(entity, out var currentBehavior))
        {
            var newBehavior = new FlexPositionTopBehavior(0, boolValue);
            entity.SetBehavior<FlexPositionTopBehavior, FlexPositionTopBehavior>(
                in newBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        else
        {
            var updatedBehavior = new FlexPositionTopBehavior(currentBehavior.Value.Value, boolValue);
            entity.SetBehavior<FlexPositionTopBehavior, FlexPositionTopBehavior>(
                in updatedBehavior,
                static (ref readonly _b, ref b) => b = _b
            );
        }
        return true;
    }

    // ========== Int Behavior ==========

    /// <summary>
    /// Applies FlexZOverride mutation.
    /// </summary>
    private static bool ApplyFlexZOverrideMutation(ushort entityIndex, JsonNode value) =>
        ApplyIntBehavior<FlexZOverride>(
            entityIndex,
            value,
            "flexZOverride",
            static v => new FlexZOverride(v)
        );

    // ========== Helper Methods ==========

    /// <summary>
    /// Generic helper for applying single-value float behaviors.
    /// Eliminates duplication across Border, Margin, Padding, and Grow mutations.
    /// </summary>
    private static bool ApplyFloatBehavior<TBehavior>(
        ushort entityIndex, 
        JsonNode value, 
        string fieldName,
        Func<float, TBehavior> behaviorConstructor
    )
        where TBehavior : struct
    {
        if (!value.TryGetFloatWithError(entityIndex, fieldName, out var floatValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = behaviorConstructor(floatValue);
        entity.SetBehavior<TBehavior, TBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Generic helper for applying single-value int behaviors.
    /// Eliminates duplication for integer-based mutations.
    /// </summary>
    private static bool ApplyIntBehavior<TBehavior>(
        ushort entityIndex, 
        JsonNode value, 
        string fieldName,
        Func<int, TBehavior> behaviorConstructor
    )
        where TBehavior : struct
    {
        if (!value.TryGetIntWithError(entityIndex, fieldName, out var intValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = behaviorConstructor(intValue);
        entity.SetBehavior<TBehavior, TBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Generic helper for applying enum-based behaviors.
    /// Eliminates duplication across AlignItems, Direction, Wrap, etc.
    /// </summary>
    private static bool ApplyEnumBehavior<TBehavior, TEnum>(
        ushort entityIndex, 
        JsonNode value, 
        string fieldName,
        Func<TEnum, TBehavior> behaviorConstructor
    )
        where TBehavior : struct
        where TEnum : struct, Enum
    {
        if (!value.TryGetEnumWithError<TEnum>(entityIndex, fieldName, out var enumValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = behaviorConstructor(enumValue);
        entity.SetBehavior<TBehavior, TBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Tries to apply JsonLogic to a rule or expression and catch any exceptions.
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
}
