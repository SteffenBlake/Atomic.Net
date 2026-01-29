using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Flex;
using Atomic.Net.MonoGame.Hierarchy;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Tags;
using Atomic.Net.MonoGame.Transform;
using FlexLayoutSharp;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Helper for building entity JsonNodes with all behaviors.
/// Extracted from RulesDriver for reuse by SequenceDriver.
/// </summary>
public static class EntityJsonHelper
{
    /// <summary>
    /// Builds a JsonObject representation of an entity with all its behaviors.
    /// </summary>
    public static JsonObject BuildEntityJsonNode(Entity entity)
    {
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

        return entityObj;
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
}
