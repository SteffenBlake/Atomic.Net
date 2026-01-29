using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
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

    private readonly List<string> _tempTagsList = new(32);
    
    // senior-dev: Pre-allocated builder to reduce allocations when creating ImmutableHashSet
    // ToImmutable() still allocates, but reusing the builder is more efficient than ToImmutableHashSet()
    private readonly ImmutableHashSet<string>.Builder _tagsBuilder = ImmutableHashSet.CreateBuilder<string>();

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

        if (targetObj.TryGetPropertyValue("properties", out var propertyKeyNode) && propertyKeyNode != null)
        {
            return ApplyPropertyMutation(entityIndex, propertyKeyNode, value);
        }
        else if (targetObj.TryGetPropertyValue("id", out _))
        {
            return ApplyIdMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("tags", out _))
        {
            return ApplyTagsMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("transform", out var transformField) && transformField != null)
        {
            return ApplyTransformMutation(entityIndex, transformField, value);
        }
        else if (targetObj.TryGetPropertyValue("parent", out _))
        {
            return ApplyParentMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexAlignItems", out _))
        {
            return ApplyFlexAlignItemsMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexAlignSelf", out _))
        {
            return ApplyFlexAlignSelfMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexBorderBottom", out _))
        {
            return ApplyFlexBorderBottomMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexBorderLeft", out _))
        {
            return ApplyFlexBorderLeftMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexBorderRight", out _))
        {
            return ApplyFlexBorderRightMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexBorderTop", out _))
        {
            return ApplyFlexBorderTopMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexDirection", out _))
        {
            return ApplyFlexDirectionMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexGrow", out _))
        {
            return ApplyFlexGrowMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexWrap", out _))
        {
            return ApplyFlexWrapMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexZOverride", out _))
        {
            return ApplyFlexZOverrideMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexHeight", out _))
        {
            return ApplyFlexHeightMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexJustifyContent", out _))
        {
            return ApplyFlexJustifyContentMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexMarginBottom", out _))
        {
            return ApplyFlexMarginBottomMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexMarginLeft", out _))
        {
            return ApplyFlexMarginLeftMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexMarginRight", out _))
        {
            return ApplyFlexMarginRightMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexMarginTop", out _))
        {
            return ApplyFlexMarginTopMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexPaddingBottom", out _))
        {
            return ApplyFlexPaddingBottomMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexPaddingLeft", out _))
        {
            return ApplyFlexPaddingLeftMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexPaddingRight", out _))
        {
            return ApplyFlexPaddingRightMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexPaddingTop", out _))
        {
            return ApplyFlexPaddingTopMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexPositionBottom", out _))
        {
            return ApplyFlexPositionBottomMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexPositionLeft", out _))
        {
            return ApplyFlexPositionLeftMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexPositionRight", out _))
        {
            return ApplyFlexPositionRightMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexPositionTop", out _))
        {
            return ApplyFlexPositionTopMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexPositionType", out _))
        {
            return ApplyFlexPositionTypeMutation(entityIndex, value);
        }
        else if (targetObj.TryGetPropertyValue("flexWidth", out _))
        {
            return ApplyFlexWidthMutation(entityIndex, value);
        }

        var validTargets = string.Join(", ", new[]
        {
            "properties", "id", "tags", "transform", "parent",
            "flexAlignItems", "flexAlignSelf",
            "flexBorderBottom", "flexBorderLeft", "flexBorderRight", "flexBorderTop",
            "flexDirection", "flexGrow", "flexWrap", "flexZOverride",
            "flexHeight", "flexJustifyContent",
            "flexMarginBottom", "flexMarginLeft", "flexMarginRight", "flexMarginTop",
            "flexPaddingBottom", "flexPaddingLeft", "flexPaddingRight", "flexPaddingTop",
            "flexPositionBottom", "flexPositionLeft", "flexPositionRight", "flexPositionTop", "flexPositionType",
            "flexWidth"
        });
        EventBus<ErrorEvent>.Push(new ErrorEvent(
            $"Unrecognized target for entity {entityIndex}. Expected one of: {validTargets}"
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
        string newId;
        try
        {
            newId = value.GetValue<string>();
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse id value for entity {entityIndex}: {ex.Message}. Id must be a string."
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

        _tempTagsList.Clear();
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

            string tag;
            try
            {
                tag = tagNode.GetValue<string>();
            }
            catch (Exception ex)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Failed to parse tag at index {i} for entity {entityIndex}: {ex.Message}. Tags must be strings."
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

            _tempTagsList.Add(tag);
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Cannot mutate inactive entity {entityIndex}"
            ));
            return false;
        }

        _tagsBuilder.Clear();
        _tagsBuilder.UnionWith(_tempTagsList);
        var tagsToSet = _tagsBuilder.ToImmutable();
        entity.SetBehavior<TagsBehavior, ImmutableHashSet<string>>(
            in tagsToSet,
            static (ref readonly _tags, ref b) => b = b with { Tags = _tags }
        );

        return true;
    }

    /// <summary>
    /// Applies a Transform mutation to an entity.
    /// Handles position, rotation, scale, and anchor fields.
    /// </summary>
    private static bool ApplyTransformMutation(ushort entityIndex, JsonNode field, JsonNode value)
    {
        if (field is not JsonObject fieldObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Transform field must be a JsonObject for entity {entityIndex}"
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

        if (fieldObj.TryGetPropertyValue("position", out _))
        {
            if (!TryParseVector3(value, entityIndex, "position", out var vector))
            {
                return false;
            }
            entity.SetBehavior<TransformBehavior, Vector3>(
                in vector,
                static (ref readonly _vec, ref b) => b.Position = _vec
            );
            return true;
        }
        else if (fieldObj.TryGetPropertyValue("rotation", out _))
        {
            if (!TryParseQuaternion(value, entityIndex, "rotation", out var quat))
            {
                return false;
            }
            entity.SetBehavior<TransformBehavior, Quaternion>(
                in quat,
                static (ref readonly _quat, ref b) => b.Rotation = _quat
            );
            return true;
        }
        else if (fieldObj.TryGetPropertyValue("scale", out _))
        {
            if (!TryParseVector3(value, entityIndex, "scale", out var vector))
            {
                return false;
            }
            entity.SetBehavior<TransformBehavior, Vector3>(
                in vector,
                static (ref readonly _vec, ref b) => b.Scale = _vec
            );
            return true;
        }
        else if (fieldObj.TryGetPropertyValue("anchor", out _))
        {
            if (!TryParseVector3(value, entityIndex, "anchor", out var vector))
            {
                return false;
            }
            entity.SetBehavior<TransformBehavior, Vector3>(
                in vector,
                static (ref readonly _vec, ref b) => b.Anchor = _vec
            );
            return true;
        }

        EventBus<ErrorEvent>.Push(new ErrorEvent(
            $"Transform mutation must specify position, rotation, scale, or anchor for entity {entityIndex}"
        ));
        return false;
    }

    /// <summary>
    /// Tries to parse a Vector3 from a JsonNode.
    /// </summary>
    private static bool TryParseVector3(JsonNode value, ushort entityIndex, string fieldName, out Vector3 result)
    {
        if (value is not JsonObject vecObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"{fieldName} must be a JsonObject with x, y, z fields for entity {entityIndex}"
            ));
            result = default;
            return false;
        }

        try
        {
            var x = vecObj["x"]!.GetValue<float>();
            var y = vecObj["y"]!.GetValue<float>();
            var z = vecObj["z"]!.GetValue<float>();
            result = new Vector3(x, y, z);
            return true;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} for entity {entityIndex}: {ex.Message}"
            ));
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Tries to parse a Quaternion from a JsonNode.
    /// </summary>
    private static bool TryParseQuaternion(JsonNode value, ushort entityIndex, string fieldName, out Quaternion result)
    {
        if (value is not JsonObject quatObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"{fieldName} must be a JsonObject with x, y, z, w fields for entity {entityIndex}"
            ));
            result = default;
            return false;
        }

        try
        {
            var x = quatObj["x"]!.GetValue<float>();
            var y = quatObj["y"]!.GetValue<float>();
            var z = quatObj["z"]!.GetValue<float>();
            var w = quatObj["w"]!.GetValue<float>();
            result = new Quaternion(x, y, z, w);
            return true;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} for entity {entityIndex}: {ex.Message}"
            ));
            result = default;
            return false;
        }
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
    private static bool ApplyFlexAlignItemsMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseEnum<Align>(value, entityIndex, "flexAlignItems", out var enumValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexAlignItemsBehavior(enumValue);
        entity.SetBehavior<FlexAlignItemsBehavior, FlexAlignItemsBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexAlignSelf mutation.
    /// </summary>
    private static bool ApplyFlexAlignSelfMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseEnum<Align>(value, entityIndex, "flexAlignSelf", out var enumValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexAlignSelfBehavior(enumValue);
        entity.SetBehavior<FlexAlignSelfBehavior, FlexAlignSelfBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexDirection mutation.
    /// </summary>
    private static bool ApplyFlexDirectionMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseEnum<FlexDirection>(value, entityIndex, "flexDirection", out var enumValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexDirectionBehavior(enumValue);
        entity.SetBehavior<FlexDirectionBehavior, FlexDirectionBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexWrap mutation.
    /// </summary>
    private static bool ApplyFlexWrapMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseEnum<Wrap>(value, entityIndex, "flexWrap", out var enumValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexWrapBehavior(enumValue);
        entity.SetBehavior<FlexWrapBehavior, FlexWrapBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexJustifyContent mutation.
    /// </summary>
    private static bool ApplyFlexJustifyContentMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseEnum<Justify>(value, entityIndex, "flexJustifyContent", out var enumValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexJustifyContentBehavior(enumValue);
        entity.SetBehavior<FlexJustifyContentBehavior, FlexJustifyContentBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexPositionType mutation.
    /// </summary>
    private static bool ApplyFlexPositionTypeMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseEnum<PositionType>(value, entityIndex, "flexPositionType", out var enumValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexPositionTypeBehavior(enumValue);
        entity.SetBehavior<FlexPositionTypeBehavior, FlexPositionTypeBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    // ========== Simple Float Behaviors ==========

    /// <summary>
    /// Applies FlexBorderBottom mutation.
    /// </summary>
    private static bool ApplyFlexBorderBottomMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseFloat(value, entityIndex, "flexBorderBottom", out var floatValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexBorderBottomBehavior(floatValue);
        entity.SetBehavior<FlexBorderBottomBehavior, FlexBorderBottomBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexBorderLeft mutation.
    /// </summary>
    private static bool ApplyFlexBorderLeftMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseFloat(value, entityIndex, "flexBorderLeft", out var floatValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexBorderLeftBehavior(floatValue);
        entity.SetBehavior<FlexBorderLeftBehavior, FlexBorderLeftBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexBorderRight mutation.
    /// </summary>
    private static bool ApplyFlexBorderRightMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseFloat(value, entityIndex, "flexBorderRight", out var floatValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexBorderRightBehavior(floatValue);
        entity.SetBehavior<FlexBorderRightBehavior, FlexBorderRightBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexBorderTop mutation.
    /// </summary>
    private static bool ApplyFlexBorderTopMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseFloat(value, entityIndex, "flexBorderTop", out var floatValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexBorderTopBehavior(floatValue);
        entity.SetBehavior<FlexBorderTopBehavior, FlexBorderTopBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexGrow mutation.
    /// </summary>
    private static bool ApplyFlexGrowMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseFloat(value, entityIndex, "flexGrow", out var floatValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexGrowBehavior(floatValue);
        entity.SetBehavior<FlexGrowBehavior, FlexGrowBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexMarginBottom mutation.
    /// </summary>
    private static bool ApplyFlexMarginBottomMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseFloat(value, entityIndex, "flexMarginBottom", out var floatValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexMarginBottomBehavior(floatValue);
        entity.SetBehavior<FlexMarginBottomBehavior, FlexMarginBottomBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexMarginLeft mutation.
    /// </summary>
    private static bool ApplyFlexMarginLeftMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseFloat(value, entityIndex, "flexMarginLeft", out var floatValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexMarginLeftBehavior(floatValue);
        entity.SetBehavior<FlexMarginLeftBehavior, FlexMarginLeftBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexMarginRight mutation.
    /// </summary>
    private static bool ApplyFlexMarginRightMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseFloat(value, entityIndex, "flexMarginRight", out var floatValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexMarginRightBehavior(floatValue);
        entity.SetBehavior<FlexMarginRightBehavior, FlexMarginRightBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexMarginTop mutation.
    /// </summary>
    private static bool ApplyFlexMarginTopMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseFloat(value, entityIndex, "flexMarginTop", out var floatValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexMarginTopBehavior(floatValue);
        entity.SetBehavior<FlexMarginTopBehavior, FlexMarginTopBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexPaddingBottom mutation.
    /// </summary>
    private static bool ApplyFlexPaddingBottomMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseFloat(value, entityIndex, "flexPaddingBottom", out var floatValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexPaddingBottomBehavior(floatValue);
        entity.SetBehavior<FlexPaddingBottomBehavior, FlexPaddingBottomBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexPaddingLeft mutation.
    /// </summary>
    private static bool ApplyFlexPaddingLeftMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseFloat(value, entityIndex, "flexPaddingLeft", out var floatValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexPaddingLeftBehavior(floatValue);
        entity.SetBehavior<FlexPaddingLeftBehavior, FlexPaddingLeftBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexPaddingRight mutation.
    /// </summary>
    private static bool ApplyFlexPaddingRightMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseFloat(value, entityIndex, "flexPaddingRight", out var floatValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexPaddingRightBehavior(floatValue);
        entity.SetBehavior<FlexPaddingRightBehavior, FlexPaddingRightBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexPaddingTop mutation.
    /// </summary>
    private static bool ApplyFlexPaddingTopMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseFloat(value, entityIndex, "flexPaddingTop", out var floatValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexPaddingTopBehavior(floatValue);
        entity.SetBehavior<FlexPaddingTopBehavior, FlexPaddingTopBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    // ========== Two-Field Behaviors (Value + Percent) ==========

    /// <summary>
    /// Applies FlexHeight mutation.
    /// </summary>
    private static bool ApplyFlexHeightMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseTwoFieldBehavior(value, entityIndex, "flexHeight", out var floatValue, out var percentValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexHeightBehavior(floatValue, percentValue);
        entity.SetBehavior<FlexHeightBehavior, FlexHeightBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexWidth mutation.
    /// </summary>
    private static bool ApplyFlexWidthMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseTwoFieldBehavior(value, entityIndex, "flexWidth", out var floatValue, out var percentValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexWidthBehavior(floatValue, percentValue);
        entity.SetBehavior<FlexWidthBehavior, FlexWidthBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexPositionBottom mutation.
    /// </summary>
    private static bool ApplyFlexPositionBottomMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseTwoFieldBehavior(value, entityIndex, "flexPositionBottom", out var floatValue, out var percentValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexPositionBottomBehavior(floatValue, percentValue);
        entity.SetBehavior<FlexPositionBottomBehavior, FlexPositionBottomBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexPositionLeft mutation.
    /// </summary>
    private static bool ApplyFlexPositionLeftMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseTwoFieldBehavior(value, entityIndex, "flexPositionLeft", out var floatValue, out var percentValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexPositionLeftBehavior(floatValue, percentValue);
        entity.SetBehavior<FlexPositionLeftBehavior, FlexPositionLeftBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexPositionRight mutation.
    /// </summary>
    private static bool ApplyFlexPositionRightMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseTwoFieldBehavior(value, entityIndex, "flexPositionRight", out var floatValue, out var percentValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexPositionRightBehavior(floatValue, percentValue);
        entity.SetBehavior<FlexPositionRightBehavior, FlexPositionRightBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    /// <summary>
    /// Applies FlexPositionTop mutation.
    /// </summary>
    private static bool ApplyFlexPositionTopMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseTwoFieldBehavior(value, entityIndex, "flexPositionTop", out var floatValue, out var percentValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexPositionTopBehavior(floatValue, percentValue);
        entity.SetBehavior<FlexPositionTopBehavior, FlexPositionTopBehavior>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    // ========== Int Behavior ==========

    /// <summary>
    /// Applies FlexZOverride mutation.
    /// </summary>
    private static bool ApplyFlexZOverrideMutation(ushort entityIndex, JsonNode value)
    {
        if (!TryParseInt(value, entityIndex, "flexZOverride", out var intValue))
        {
            return false;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot mutate inactive entity {entityIndex}"));
            return false;
        }

        var behavior = new FlexZOverride(intValue);
        entity.SetBehavior<FlexZOverride, FlexZOverride>(
            in behavior,
            static (ref readonly _b, ref b) => b = _b
        );
        return true;
    }

    // ========== Helper Methods ==========

    /// <summary>
    /// Tries to parse an enum value from a JsonNode.
    /// </summary>
    private static bool TryParseEnum<TEnum>(JsonNode value, ushort entityIndex, string fieldName, out TEnum result)
        where TEnum : struct, Enum
    {
        try
        {
            var stringValue = value.GetValue<string>();
            if (Enum.TryParse<TEnum>(stringValue, ignoreCase: true, out result))
            {
                return true;
            }

            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Invalid {fieldName} value '{stringValue}' for entity {entityIndex}. Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}"
            ));
            return false;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} for entity {entityIndex}: {ex.Message}"
            ));
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Tries to parse a float value from a JsonNode.
    /// </summary>
    private static bool TryParseFloat(JsonNode value, ushort entityIndex, string fieldName, out float result)
    {
        try
        {
            result = value.GetValue<float>();
            return true;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} for entity {entityIndex}: {ex.Message}. Expected a numeric value."
            ));
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Tries to parse an int value from a JsonNode.
    /// </summary>
    private static bool TryParseInt(JsonNode value, ushort entityIndex, string fieldName, out int result)
    {
        try
        {
            result = value.GetValue<int>();
            return true;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} for entity {entityIndex}: {ex.Message}. Expected an integer value."
            ));
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Tries to parse a two-field behavior (value + percent) from a JsonNode.
    /// </summary>
    private static bool TryParseTwoFieldBehavior(JsonNode value, ushort entityIndex, string fieldName, out float floatValue, out bool percentValue)
    {
        if (value is not JsonObject obj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"{fieldName} must be a JsonObject with 'value' and 'percent' fields for entity {entityIndex}"
            ));
            floatValue = default;
            percentValue = default;
            return false;
        }

        try
        {
            floatValue = obj["value"]!.GetValue<float>();
            percentValue = obj["percent"]!.GetValue<bool>();
            return true;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} for entity {entityIndex}: {ex.Message}"
            ));
            floatValue = default;
            percentValue = default;
            return false;
        }
    }
}
