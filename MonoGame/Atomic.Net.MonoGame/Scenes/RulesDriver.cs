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

        // Pre-create DO context template to avoid per-entity allocations
        var doContext = new JsonObject
        {
            ["world"] = new JsonObject { ["deltaTime"] = deltaTime },
            ["entities"] = filteredEntities
        };

        for (var i = 0; i < filteredEntities.Count; i++)
        {
            var entity = filteredEntities[i];
            // JsonLogic filter can return null entries in sparse results
            if (entity == null)
            {
                continue;
            }

            // Update DO context for this entity
            doContext["index"] = i;
            doContext["self"] = entity.DeepClone();  // Clone to avoid parent conflict
            
            // Execute the scene command on this entity
            rule.Do.Execute(entity, doContext);
            
            // Remove self to allow reassignment in next iteration
            doContext.Remove("self");
            
            // Write mutations back to real entity
            if (TryGetEntityIndex(entity, out var entityIndex))
            {
                WriteEntityChanges(entity, entityIndex.Value);
            }
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

