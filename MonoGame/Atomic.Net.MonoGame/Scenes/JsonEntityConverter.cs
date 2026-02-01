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

using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Handles conversion between Entity and JsonNode representations.
/// Read: Entity → JsonNode (for JsonLogic evaluation)
/// Write: JsonNode → Entity (applying mutations back to real entities)
/// </summary>
public static class JsonEntityConverter
{
    /// <summary>
    /// Converts an Entity to its JsonNode representation for JsonLogic evaluation.
    /// </summary>
    public static JsonNode Read(Entity entity)
    {
        var entityObj = new JsonObject
        {
            ["_index"] = entity.Index
        };

        if (entity.TryGetBehavior<IdBehavior>(out var idBehavior))
        {
            entityObj["id"] = idBehavior.Value.Id;
        }

        if (entity.TryGetBehavior<TagsBehavior>(out var tagBehavior))
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

        if (entity.TryGetBehavior<PropertiesBehavior>(out var propertiesBehavior))
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

        if (entity.TryGetBehavior<TransformBehavior>(out var transformBehavior))
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

        if (entity.TryGetBehavior<ParentBehavior>(out var parentBehavior))
        {
            entityObj["parent"] = parentBehavior.Value.ParentSelector.ToString();
        }


        // Flex behaviors
        if (entity.TryGetBehavior<FlexAlignItemsBehavior>(out var alignItems))
        {
            entityObj["flexAlignItems"] = alignItems.Value.Value.ToString();
        }

        if (entity.TryGetBehavior<FlexAlignSelfBehavior>(out var alignSelf))
        {
            entityObj["flexAlignSelf"] = alignSelf.Value.Value.ToString();
        }

        if (entity.TryGetBehavior<FlexBorderBottomBehavior>(out var borderBottom))
        {
            entityObj["flexBorderBottom"] = borderBottom.Value.Value;
        }

        if (entity.TryGetBehavior<FlexBorderLeftBehavior>(out var borderLeft))
        {
            entityObj["flexBorderLeft"] = borderLeft.Value.Value;
        }

        if (entity.TryGetBehavior<FlexBorderRightBehavior>(out var borderRight))
        {
            entityObj["flexBorderRight"] = borderRight.Value.Value;
        }

        if (entity.TryGetBehavior<FlexBorderTopBehavior>(out var borderTop))
        {
            entityObj["flexBorderTop"] = borderTop.Value.Value;
        }

        if (entity.TryGetBehavior<FlexDirectionBehavior>(out var direction))
        {
            entityObj["flexDirection"] = direction.Value.Value.ToString();
        }

        if (entity.TryGetBehavior<FlexGrowBehavior>(out var grow))
        {
            entityObj["flexGrow"] = grow.Value.Value;
        }

        if (entity.TryGetBehavior<FlexWrapBehavior>(out var wrap))
        {
            entityObj["flexWrap"] = wrap.Value.Value.ToString();
        }

        if (entity.TryGetBehavior<FlexZOverride>(out var zOverride))
        {
            entityObj["flexZOverride"] = zOverride.Value.ZIndex;
        }

        if (entity.TryGetBehavior<FlexHeightBehavior>(out var height))
        {
            entityObj["flexHeight"] = new JsonObject
            {
                ["value"] = height.Value.Value,
                ["percent"] = height.Value.Percent
            };
        }

        if (entity.TryGetBehavior<FlexJustifyContentBehavior>(out var justifyContent))
        {
            entityObj["flexJustifyContent"] = justifyContent.Value.Value.ToString();
        }

        if (entity.TryGetBehavior<FlexMarginBottomBehavior>(out var marginBottom))
        {
            entityObj["flexMarginBottom"] = marginBottom.Value.Value;
        }

        if (entity.TryGetBehavior<FlexMarginLeftBehavior>(out var marginLeft))
        {
            entityObj["flexMarginLeft"] = marginLeft.Value.Value;
        }

        if (entity.TryGetBehavior<FlexMarginRightBehavior>(out var marginRight))
        {
            entityObj["flexMarginRight"] = marginRight.Value.Value;
        }

        if (entity.TryGetBehavior<FlexMarginTopBehavior>(out var marginTop))
        {
            entityObj["flexMarginTop"] = marginTop.Value.Value;
        }

        if (entity.TryGetBehavior<FlexPaddingBottomBehavior>(out var paddingBottom))
        {
            entityObj["flexPaddingBottom"] = paddingBottom.Value.Value;
        }

        if (entity.TryGetBehavior<FlexPaddingLeftBehavior>(out var paddingLeft))
        {
            entityObj["flexPaddingLeft"] = paddingLeft.Value.Value;
        }

        if (entity.TryGetBehavior<FlexPaddingRightBehavior>(out var paddingRight))
        {
            entityObj["flexPaddingRight"] = paddingRight.Value.Value;
        }

        if (entity.TryGetBehavior<FlexPaddingTopBehavior>(out var paddingTop))
        {
            entityObj["flexPaddingTop"] = paddingTop.Value.Value;
        }

        if (entity.TryGetBehavior<FlexPositionBottomBehavior>(out var positionBottom))
        {
            entityObj["flexPositionBottom"] = new JsonObject
            {
                ["value"] = positionBottom.Value.Value,
                ["percent"] = positionBottom.Value.Percent
            };
        }

        if (entity.TryGetBehavior<FlexPositionLeftBehavior>(out var positionLeft))
        {
            entityObj["flexPositionLeft"] = new JsonObject
            {
                ["value"] = positionLeft.Value.Value,
                ["percent"] = positionLeft.Value.Percent
            };
        }

        if (entity.TryGetBehavior<FlexPositionRightBehavior>(out var positionRight))
        {
            entityObj["flexPositionRight"] = new JsonObject
            {
                ["value"] = positionRight.Value.Value,
                ["percent"] = positionRight.Value.Percent
            };
        }

        if (entity.TryGetBehavior<FlexPositionTopBehavior>(out var positionTop))
        {
            entityObj["flexPositionTop"] = new JsonObject
            {
                ["value"] = positionTop.Value.Value,
                ["percent"] = positionTop.Value.Percent
            };
        }

        if (entity.TryGetBehavior<FlexPositionTypeBehavior>(out var positionType))
        {
            entityObj["flexPositionType"] = positionType.Value.Value.ToString();
        }

        if (entity.TryGetBehavior<FlexWidthBehavior>(out var width))
        {
            entityObj["flexWidth"] = new JsonObject
            {
                ["value"] = width.Value.Value,
                ["percent"] = width.Value.Percent
            };
        }

        return entityObj;
    }

    /// <summary>
    /// Writes mutations from a JsonNode back to the real Entity.
    /// Uses direct JsonNode access patterns without MutEntity deserialization for optimal performance.
    /// All fields are processed uniformly using null-safe JsonNode navigation.
    /// </summary>
    public static void Write(JsonNode jsonEntity, Entity entity)
    {
        if (jsonEntity is not JsonObject entityObj)
        {
            return;
        }

        TrySetRef<string, IdBehavior>(
            entity,
            entityObj["id"],
            static (ref readonly _newId, ref b) => b = new IdBehavior(_newId)
        );

        if (entityObj.TryGetPropertyValue("tags", out var tagsNode) && tagsNode is JsonArray tagsArray)
        {
            entity.SetBehavior<TagsBehavior>(static (ref b) => b = b with
            {
                Tags = b.Tags.Clear()
            });

            foreach (var tagNode in tagsArray)
            {
                if (tagNode == null)
                {
                    EventBus<ErrorEvent>.Push(
                        new ErrorEvent("Tags mutation failed: tag cannot be null")
                    );
                    continue;
                }

                if (!tagNode.TryGetStringValue(out var tag))
                {
                    EventBus<ErrorEvent>.Push(
                        new ErrorEvent($"Tags mutation failed: expected string, got {tagNode.GetValueKind()}")
                    );
                    continue;
                }

                entity.SetBehavior<TagsBehavior, string>(
                    in tag,
                    static (ref readonly _tag, ref b) => b = b with
                    {
                        Tags = b.Tags.With(_tag)
                    }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("properties", out var propertiesNode) &&
            propertiesNode is JsonObject propertiesObj)
        {
            foreach (var kvp in propertiesObj)
            {
                var key = kvp.Key;
                var valueNode = kvp.Value;

                if (valueNode is not JsonValue jsonValue)
                {
                    continue;
                }

                PropertyValue? propValue = null;

                if (jsonValue.TryGetValue<string>(out var strValue))
                {
                    propValue = strValue;
                }
                else if (jsonValue.TryCoerceFloatValue(out var floatValue))
                {
                    propValue = floatValue.Value;
                }
                else if (jsonValue.TryGetValue<bool>(out var boolValue))
                {
                    propValue = boolValue;
                }

                if (!propValue.HasValue)
                {
                    continue;
                }

                var data = (key, propValue.Value);
                entity.SetBehavior<PropertiesBehavior, (string Key, PropertyValue Value)>(
                    in data,
                    static (ref readonly _data, ref b) =>
                        b = b with
                        {
                            Properties = b.Properties.With(_data.Key, _data.Value)
                        }
                );
            }
        }

        if (entityObj["transform"] is JsonNode transform)
        {
            if (transform["position"] is JsonNode position)
            {
                TrySetStruct<float, TransformBehavior>(
                    entity,
                    position["x"],
                    static (ref readonly _x, ref b) => b = b with
                    {
                        Position = b.Position with { X = _x }
                    }
                );

                TrySetStruct<float, TransformBehavior>(
                    entity,
                    position["y"],
                    static (ref readonly _y, ref b) => b = b with
                    {
                        Position = b.Position with { Y = _y }
                    }
                );

                TrySetStruct<float, TransformBehavior>(
                    entity,
                    position["z"],
                    static (ref readonly _z, ref b) => b = b with
                    {
                        Position = b.Position with { Z = _z }
                    }
                );
            }

            if (transform["rotation"] is JsonNode rotation)
            {
                TrySetStruct<float, TransformBehavior>(
                    entity,
                    rotation["x"],
                    static (ref readonly _x, ref b) => b = b with
                    {
                        Rotation = b.Rotation with { X = _x }
                    }
                );

                TrySetStruct<float, TransformBehavior>(
                    entity,
                    rotation["y"],
                    static (ref readonly _y, ref b) => b = b with
                    {
                        Rotation = b.Rotation with { Y = _y }
                    }
                );

                TrySetStruct<float, TransformBehavior>(
                    entity,
                    rotation["z"],
                    static (ref readonly _z, ref b) => b = b with
                    {
                        Rotation = b.Rotation with { Z = _z }
                    }
                );

                TrySetStruct<float, TransformBehavior>(
                    entity,
                    rotation["w"],
                    static (ref readonly _w, ref b) => b = b with
                    {
                        Rotation = b.Rotation with { W = _w }
                    }
                );
            }

            if (transform["scale"] is JsonNode scale)
            {
                TrySetStruct<float, TransformBehavior>(
                    entity,
                    scale["x"],
                    static (ref readonly _x, ref b) => b = b with
                    {
                        Scale = b.Scale with { X = _x }
                    }
                );

                TrySetStruct<float, TransformBehavior>(
                    entity,
                    scale["y"],
                    static (ref readonly _y, ref b) => b = b with
                    {
                        Scale = b.Scale with { Y = _y }
                    }
                );

                TrySetStruct<float, TransformBehavior>(
                    entity,
                    scale["z"],
                    static (ref readonly _z, ref b) => b = b with
                    {
                        Scale = b.Scale with { Z = _z }
                    }
                );
            }

            if (transform["anchor"] is JsonNode anchor)
            {
                TrySetStruct<float, TransformBehavior>(
                    entity,
                    anchor["x"],
                    static (ref readonly _x, ref b) => b = b with
                    {
                        Anchor = b.Anchor with { X = _x }
                    }
                );

                TrySetStruct<float, TransformBehavior>(
                    entity,
                    anchor["y"],
                    static (ref readonly _y, ref b) => b = b with
                    {
                        Anchor = b.Anchor with { Y = _y }
                    }
                );

                TrySetStruct<float, TransformBehavior>(
                    entity,
                    anchor["z"],
                    static (ref readonly _z, ref b) => b = b with
                    {
                        Anchor = b.Anchor with { Z = _z }
                    }
                );
            }
        }

        TrySetEntitySelector(entity, entityObj);

        TrySetEnum<Align, FlexAlignItemsBehavior>(
            entity,
            entityObj["flexAlignItems"],
            static (ref readonly _alignItems, ref b) =>
                b = new FlexAlignItemsBehavior(_alignItems)
        );

        TrySetEnum<Align, FlexAlignSelfBehavior>(
            entity,
            entityObj["flexAlignSelf"],
            static (ref readonly _alignSelf, ref b) =>
                b = new FlexAlignSelfBehavior(_alignSelf)
        );

        TrySetStruct<float, FlexBorderBottomBehavior>(
            entity,
            entityObj["flexBorderBottom"],
            static (ref readonly _borderBottom, ref b) =>
                    b = new FlexBorderBottomBehavior(_borderBottom)
        );

        TrySetStruct<float, FlexBorderLeftBehavior>(
            entity,
            entityObj["flexBorderLeft"],
            static (ref readonly _borderLeft, ref b) =>
                    b = new FlexBorderLeftBehavior(_borderLeft)
        );

        TrySetStruct<float, FlexBorderRightBehavior>(
            entity,
            entityObj["flexBorderRight"],
            static (ref readonly _borderRight, ref b) =>
                    b = new FlexBorderRightBehavior(_borderRight)
        );

        TrySetStruct<float, FlexBorderTopBehavior>(
            entity,
            entityObj["flexBorderTop"],
            static (ref readonly _borderTop, ref b) =>
                    b = new FlexBorderTopBehavior(_borderTop)
        );

        TrySetEnum<FlexDirection, FlexDirectionBehavior>(
            entity,
            entityObj["flexDirection"],
            static (ref readonly _direction, ref b) =>
                b = new FlexDirectionBehavior(_direction)
        );

        TrySetStruct<float, FlexGrowBehavior>(
            entity,
            entityObj["flexGrow"],
            static (ref readonly _grow, ref b) =>
                    b = new FlexGrowBehavior(_grow)
        );

        TrySetEnum<Wrap, FlexWrapBehavior>(
            entity,
            entityObj["flexWrap"],
            static (ref readonly _wrap, ref b) =>
                b = new FlexWrapBehavior(_wrap)
        );

        TrySetStruct<int, FlexZOverride>(
            entity,
            entityObj["flexZOverride"],
            static (ref readonly _zOverride, ref b) =>
                    b = new FlexZOverride(_zOverride)
        );

        if (entityObj["flexHeight"] is JsonNode flexHeight)
        {
            TrySetStruct<float, FlexHeightBehavior>(
                entity,
                flexHeight["value"],
                static (ref readonly _value, ref b) => b = b with
                {
                    Value = _value
                }
            );

            TrySetStruct<bool, FlexHeightBehavior>(
                entity,
                flexHeight["percent"],
                static (ref readonly _percent, ref b) => b = b with
                {
                    Percent = _percent
                }
            );
        }

        if (entityObj["flexWidth"] is JsonNode flexWidth)
        {
            TrySetStruct<float, FlexWidthBehavior>(
                entity,
                flexWidth["value"],
                static (ref readonly _value, ref b) => b = b with
                {
                    Value = _value
                }
            );

            TrySetStruct<bool, FlexWidthBehavior>(
                entity,
                flexWidth["percent"],
                static (ref readonly _percent, ref b) => b = b with
                {
                    Percent = _percent
                }
            );
        }

        TrySetEnum<Justify, FlexJustifyContentBehavior>(
            entity,
            entityObj["flexJustifyContent"],
            static (ref readonly _justify, ref b) =>
                b = new FlexJustifyContentBehavior(_justify)
        );

        TrySetStruct<float, FlexMarginBottomBehavior>(
            entity,
            entityObj["flexMarginBottom"],
            static (ref readonly _marginBottom, ref b) =>
                b = new FlexMarginBottomBehavior(_marginBottom)
        );

        TrySetStruct<float, FlexMarginLeftBehavior>(
            entity,
            entityObj["flexMarginLeft"],
            static (ref readonly _marginLeft, ref b) =>
                b = new FlexMarginLeftBehavior(_marginLeft)
        );

        TrySetStruct<float, FlexMarginRightBehavior>(
            entity,
            entityObj["flexMarginRight"],
            static (ref readonly _marginRight, ref b) =>
                b = new FlexMarginRightBehavior(_marginRight)
        );

        TrySetStruct<float, FlexMarginTopBehavior>(
            entity,
            entityObj["flexMarginTop"],
            static (ref readonly _marginTop, ref b) =>
                b = new FlexMarginTopBehavior(_marginTop)
        );

        TrySetStruct<float, FlexPaddingBottomBehavior>(
            entity,
            entityObj["flexPaddingBottom"],
            static (ref readonly _paddingBottom, ref b) =>
                b = new FlexPaddingBottomBehavior(_paddingBottom)
        );

        TrySetStruct<float, FlexPaddingLeftBehavior>(
            entity,
            entityObj["flexPaddingLeft"],
            static (ref readonly _paddingLeft, ref b) =>
                b = new FlexPaddingLeftBehavior(_paddingLeft)
        );

        TrySetStruct<float, FlexPaddingRightBehavior>(
            entity,
            entityObj["flexPaddingRight"],
            static (ref readonly _paddingRight, ref b) =>
                b = new FlexPaddingRightBehavior(_paddingRight)
        );

        TrySetStruct<float, FlexPaddingTopBehavior>(
            entity,
            entityObj["flexPaddingTop"],
            static (ref readonly _paddingTop, ref b) =>
                b = new FlexPaddingTopBehavior(_paddingTop)
        );

        if (entityObj["flexPositionBottom"] is JsonNode flexPositionBottom)
        {
            TrySetStruct<float, FlexPositionBottomBehavior>(
                entity,
                flexPositionBottom["value"],
                static (ref readonly _value, ref b) => b = b with
                {
                    Value = _value
                }
            );

            TrySetStruct<bool, FlexPositionBottomBehavior>(
                entity,
                flexPositionBottom["percent"],
                static (ref readonly _percent, ref b) => b = b with
                {
                    Percent = _percent
                }
            );
        }

        if (entityObj["flexPositionLeft"] is JsonNode flexPositionLeft)
        {
            TrySetStruct<float, FlexPositionLeftBehavior>(
                entity,
                flexPositionLeft["value"],
                static (ref readonly _value, ref b) => b = b with
                {
                    Value = _value
                }
            );

            TrySetStruct<bool, FlexPositionLeftBehavior>(
                entity,
                flexPositionLeft["percent"],
                static (ref readonly _percent, ref b) => b = b with
                {
                    Percent = _percent
                }
            );
        }

        if (entityObj["flexPositionRight"] is JsonNode flexPositionRight)
        {
            TrySetStruct<float, FlexPositionRightBehavior>(
                entity,
                flexPositionRight["value"],
                static (ref readonly _value, ref b) => b = b with
                {
                    Value = _value
                }
            );

            TrySetStruct<bool, FlexPositionRightBehavior>(
                entity,
                flexPositionRight["percent"],
                static (ref readonly _percent, ref b) => b = b with
                {
                    Percent = _percent
                }
            );
        }

        if (entityObj["flexPositionTop"] is JsonNode flexPositionTop)
        {
            TrySetStruct<float, FlexPositionTopBehavior>(
                entity,
                flexPositionTop["value"],
                static (ref readonly _value, ref b) => b = b with
                {
                    Value = _value
                }
            );

            TrySetStruct<bool, FlexPositionTopBehavior>(
                entity,
                flexPositionTop["percent"],
                static (ref readonly _percent, ref b) => b = b with
                {
                    Percent = _percent
                }
            );
        }

        TrySetEnum<PositionType, FlexPositionTypeBehavior>(
            entity,
            entityObj["flexPositionType"],
            static (ref readonly _flexPositionType, ref b) =>
                b = new FlexPositionTypeBehavior(_flexPositionType)
        );
    }

    private static void TrySetRef<TValue, TBehavior>(
        Entity entity,
        JsonNode? node,
        RefInAction<TBehavior, TValue> mutate
    )
        where TBehavior : struct
        where TValue : class
    {
        if (node == null)
        {
            return;
        }

        if (node is not JsonValue jsonValue)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unable to deserialize node to expected type, node was not a value type. Path:'{node?.GetPath()}'"
            ));
            return;
        }

        if (!jsonValue.TryGetValue<TValue>(out var value))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unable to deserialize node to expected type. Path:'{node?.GetPath()}' ExpectedType:'{typeof(TValue)} Actual:'{node?.GetValueKind()}'"
            ));
            return;
        }

        entity.SetBehavior(in value, mutate);
    }

    private static void TrySetEntitySelector(
        Entity entity,
        JsonObject entityObj
    )
    {
        var parentNode = entityObj["parent"];
        if (parentNode == null)
        {
            return;
        }

        if (parentNode is not JsonValue jsonValue)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unable to deserialize node to expected type, node was not a value type. Path:'{parentNode?.GetPath()}'"
            ));
            return;
        }

        if (!jsonValue.TryGetValue<string>(out var selectorString))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unable to deserialize node to expected type. Path:'{parentNode?.GetPath()}' ExpectedType:'EntitySelector' Actual:'{parentNode?.GetValueKind()}'"
            ));
            return;
        }

        if (!SelectorRegistry.Instance.TryParse(selectorString, out var selector))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unable to parse EntitySelector. Path:'{parentNode?.GetPath()}' Value:'{selectorString}'"
            ));
            return;
        }

        entity.SetBehavior<ParentBehavior, EntitySelector>(
            in selector,
            static (ref readonly _selector, ref b) => b = new ParentBehavior(_selector)
        );
    }

    private static void TrySetEnum<TEnum, TBehavior>(
        Entity entity,
        JsonNode? node,
        RefInAction<TBehavior, TEnum> mutate
    )
        where TBehavior : struct
        where TEnum : struct, Enum
    {
        if (node == null)
        {
            return;
        }

        if (node is not JsonValue jsonValue)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unable to deserialize node to expected type, node was not a value type. Path:'{node?.GetPath()}'"
            ));
            return;
        }

        if (!jsonValue.TryGetValue<string>(out var enumString))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unable to deserialize node to expected type. Path:'{node?.GetPath()}' ExpectedType:'{typeof(TEnum)} Actual:'{node?.GetValueKind()}'"
            ));
            return;
        }

        if (!Enum.TryParse<TEnum>(enumString, ignoreCase: true, out var enumValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unable to parse enum value. Path:'{node?.GetPath()}' ExpectedType:'{typeof(TEnum)}' Value:'{enumString}'"
            ));
            return;
        }

        entity.SetBehavior(in enumValue, mutate);
    }

    private static void TrySetStruct<TValue, TBehavior>(
        Entity entity,
        JsonNode? node,
        RefInAction<TBehavior, TValue> mutate
    )
        where TBehavior : struct
        where TValue : struct
    {
        if (node == null)
        {
            return;
        }

        if (node is not JsonValue jsonValue)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unable to deserialize node to expected type, node was not a value type. Path:'{node?.GetPath()}'"
            ));
            return;
        }

        if (!jsonValue.TryGetValue<TValue>(out var value))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unable to deserialize node to expected type. Path:'{node?.GetPath()}' ExpectedType:'{typeof(TValue)} Actual:'{node?.GetValueKind()}'"
            ));
            return;
        }

        entity.SetBehavior(in value, mutate);
    }


    private static JsonObject SerializeVector3(Vector3 vector)
    {
        return new JsonObject
        {
            ["x"] = vector.X,
            ["y"] = vector.Y,
            ["z"] = vector.Z
        };
    }

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
