using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
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
    /// Optimized to handle common cases (id, tags, properties) without full deserialization.
    /// Falls back to full deserialization only if Transform/Parent/Flex properties are present.
    /// </summary>
    public static void Write(JsonNode jsonEntity, Entity entity)
    {
        if (jsonEntity is not JsonObject entityObj)
        {
            return;
        }

        // Fast path: Check if we have any Transform/Parent/Flex properties
        // If not, we can skip expensive deserialization
        // Use a single pass through keys instead of multiple ContainsKey calls
        var needsFullDeserialization = false;
        foreach (var kvp in entityObj)
        {
            var key = kvp.Key;
            // Check if this key requires full deserialization
            // Fast path only handles: _index, id, tags, properties
            if (key != "_index" && key != "id" && key != "tags" && key != "properties")
            {
                needsFullDeserialization = true;
                break;
            }
        }

        if (needsFullDeserialization)
        {
            // Slow path: Full deserialization for Transform/Parent/Flex
            WriteFull(entityObj, entity);
            return;
        }

        // Fast path: Direct processing for id, tags, properties only
        
        // Id - direct read from JSON
        // Id
        TrySetRef<string, IdBehavior>(
            entity,
            entityObj["id"],
            static (ref readonly _newId, ref b) => b = new IdBehavior(_newId)
        );

        // Tags - direct read from JSON
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

        // Properties - direct read from JSON
        if (entityObj.TryGetPropertyValue("properties", out var propertiesNode) && 
            propertiesNode is JsonObject propertiesObj)
        {
            foreach (var kvp in propertiesObj)
            {
                var key = kvp.Key;
                var valueNode = kvp.Value;

                if (valueNode == null)
                {
                    continue;
                }

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
    }

    /// <summary>
    /// Full deserialization path for entities with Transform/Parent/Flex properties.
    /// </summary>
    private static void WriteFull(JsonObject entityObj, Entity entity)
    {
        // Deserialize entire entity structure at once
        if (!TryDeserializeMutEntity(entityObj, out var mutEntity))
        {
            return;
        }

        // Id
        TrySetRef_Old<string, IdBehavior>(
            entity,
            mutEntity.Value.Id,
            static (ref readonly _newId, ref b) => b = new IdBehavior(_newId)
        );

        // Tags
        if (mutEntity.Value.Tags != null)
        {
            entity.SetBehavior<TagsBehavior>(static (ref b) => b = b with
            {
                Tags = b.Tags.Clear()
            });

            foreach (var tagNode in mutEntity.Value.Tags)
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

        // Properties
        if (mutEntity.Value.Properties != null)
        {
            foreach (var kvp in mutEntity.Value.Properties)
            {
                var key = kvp.Key;
                var valueNode = kvp.Value;

                if (valueNode == null)
                {
                    continue;
                }

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

        // Transform - Position.X
        TrySetStruct_Old<float, TransformBehavior>(
            entity,
            mutEntity.Value.Transform?.Position?.X,
            static (ref readonly _x, ref b) => b = b with
            {
                Position = b.Position with { X = _x }
            }
        );


        // Transform - Position.Y
        TrySetStruct_Old<float, TransformBehavior>(
            entity,
            mutEntity.Value.Transform?.Position?.Y,
            static (ref readonly _y, ref b) => b = b with
            {
                Position = b.Position with { Y = _y }
            }
        );

        // Transform - Position.Z
        TrySetStruct_Old<float, TransformBehavior>(
            entity,
            mutEntity.Value.Transform?.Position?.Z,
            static (ref readonly _z, ref b) => b = b with
            {
                Position = b.Position with { Z = _z }
            }
        );

        // Transform - Rotation.X
        TrySetStruct_Old<float, TransformBehavior>(
            entity,
            mutEntity.Value.Transform?.Rotation?.X,
            static (ref readonly _x, ref b) => b = b with
            {
                Rotation = b.Rotation with { X = _x }
            }
        );

        // Transform - Rotation.Y
        TrySetStruct_Old<float, TransformBehavior>(
            entity,
            mutEntity.Value.Transform?.Rotation?.Y,
            static (ref readonly _y, ref b) => b = b with
            {
                Rotation = b.Rotation with { Y = _y }
            }
        );

        // Transform - Rotation.Z
        TrySetStruct_Old<float, TransformBehavior>(
            entity,
            mutEntity.Value.Transform?.Rotation?.Z,
            static (ref readonly _z, ref b) => b = b with
            {
                Rotation = b.Rotation with { Z = _z }
            }
        );

        // Transform - Rotation.W
        TrySetStruct_Old<float, TransformBehavior>(
            entity,
            mutEntity.Value.Transform?.Rotation?.W,
            static (ref readonly _w, ref b) => b = b with
            {
                Rotation = b.Rotation with { W = _w }
            }
        );

        // Transform - Scale.X
        TrySetStruct_Old<float, TransformBehavior>(
            entity,
            mutEntity.Value.Transform?.Scale?.X,
            static (ref readonly _x, ref b) => b = b with
            {
                Scale = b.Scale with { X = _x }
            }
        );

        // Transform - Scale.Y
        TrySetStruct_Old<float, TransformBehavior>(
            entity,
            mutEntity.Value.Transform?.Scale?.Y,
            static (ref readonly _y, ref b) => b = b with
            {
                Scale = b.Scale with { Y = _y }
            }
        );

        // Transform - Scale.Z
        TrySetStruct_Old<float, TransformBehavior>(
            entity,
            mutEntity.Value.Transform?.Scale?.Z,
            static (ref readonly _z, ref b) => b = b with
            {
                Scale = b.Scale with { Z = _z }
            }
        );

        // Transform - Anchor.X
        TrySetStruct_Old<float, TransformBehavior>(
            entity,
            mutEntity.Value.Transform?.Anchor?.X,
            static (ref readonly _x, ref b) => b = b with
            {
                Anchor = b.Anchor with { X = _x }
            }
        );

        // Transform - Anchor.Y
        TrySetStruct_Old<float, TransformBehavior>(
            entity,
            mutEntity.Value.Transform?.Anchor?.Y,
            static (ref readonly _y, ref b) => b = b with
            {
                Anchor = b.Anchor with { Y = _y }
            }
        );

        // Transform - Anchor.Z
        TrySetStruct_Old<float, TransformBehavior>(
            entity,
            mutEntity.Value.Transform?.Anchor?.Z,
            static (ref readonly _z, ref b) => b = b with
            {
                Anchor = b.Anchor with { Z = _z }
            }
        );

        // Parent
        TrySetRef_Old<EntitySelector, ParentBehavior>(
            entity,
            mutEntity.Value.Parent,
            static (ref readonly _selector, ref b) => b = new ParentBehavior(_selector)
        );

        // FlexAlignItems
        TrySetStruct_Old<Align, FlexAlignItemsBehavior>(
            entity,
            mutEntity.Value.FlexAlignItems,
            static (ref readonly _alignItems, ref b) =>
                b = new FlexAlignItemsBehavior(_alignItems)
        );

        // FlexAlignSelf
        TrySetStruct_Old<Align, FlexAlignSelfBehavior>(
            entity,
            mutEntity.Value.FlexAlignSelf,
            static (ref readonly _alignSelf, ref b) =>
                b = new FlexAlignSelfBehavior(_alignSelf)
        );

        // FlexBorderBottom
        TrySetStruct_Old<float, FlexBorderBottomBehavior>(
            entity,
            mutEntity.Value.FlexBorderBottom,
            static (ref readonly _borderBottom, ref b) =>
                    b = new FlexBorderBottomBehavior(_borderBottom)
        );

        // FlexBorderLeft
        TrySetStruct_Old<float, FlexBorderLeftBehavior>(
            entity,
            mutEntity.Value.FlexBorderLeft,
            static (ref readonly _borderLeft, ref b) =>
                    b = new FlexBorderLeftBehavior(_borderLeft)
        );

        // FlexBorderRight
        TrySetStruct_Old<float, FlexBorderRightBehavior>(
            entity,
            mutEntity.Value.FlexBorderRight,
            static (ref readonly _borderRight, ref b) =>
                    b = new FlexBorderRightBehavior(_borderRight)
        );

        // FlexBorderTop
        TrySetStruct_Old<float, FlexBorderTopBehavior>(
            entity,
            mutEntity.Value.FlexBorderTop,
            static (ref readonly _borderTop, ref b) =>
                    b = new FlexBorderTopBehavior(_borderTop)
        );

        // FlexDirection
        TrySetStruct_Old<FlexDirection, FlexDirectionBehavior>(
            entity,
            mutEntity.Value.FlexDirection,
            static (ref readonly _direction, ref b) =>
                b = new FlexDirectionBehavior(_direction)
        );

        // FlexGrow
        TrySetStruct_Old<float, FlexGrowBehavior>(
            entity,
            mutEntity.Value.FlexGrow,
            static (ref readonly _grow, ref b) =>
                    b = new FlexGrowBehavior(_grow)
        );

        // FlexWrap
        TrySetStruct_Old<Wrap, FlexWrapBehavior>(
            entity,
            mutEntity.Value.FlexWrap,
            static (ref readonly _wrap, ref b) =>
                b = new FlexWrapBehavior(_wrap)
        );

        // FlexZOverride
        TrySetStruct_Old<int, FlexZOverride>(
            entity,
            mutEntity.Value.FlexZOverride,
            static (ref readonly _zOverride, ref b) =>
                    b = new FlexZOverride(_zOverride)
        );

        // FlexHeight - Value
        TrySetStruct_Old<float, FlexHeightBehavior>(
            entity,
            mutEntity.Value.FlexHeight?.Value,
            static (ref readonly _value, ref b) => b = b with
            {
                Value = _value
            }
        );

        // FlexHeight - Percent
        TrySetStruct_Old<bool, FlexHeightBehavior>(
            entity,
            mutEntity.Value.FlexHeight?.Percent,
            static (ref readonly _percent, ref b) => b = b with
            {
                Percent = _percent
            }
        );

        // FlexJustifyContent
        TrySetStruct_Old<Justify, FlexJustifyContentBehavior>(
            entity,
            mutEntity.Value.FlexJustifyContent,
            static (ref readonly _justify, ref b) =>
                b = new FlexJustifyContentBehavior(_justify)
        );

        // FlexMarginBottom
        TrySetStruct_Old<float, FlexMarginBottomBehavior>(
            entity,
            mutEntity.Value.FlexMarginBottom,
            static (ref readonly _marginBottom, ref b) => 
                b = new FlexMarginBottomBehavior(_marginBottom)
        );

        // FlexMarginLeft
        TrySetStruct_Old<float, FlexMarginLeftBehavior>(
            entity,
            mutEntity.Value.FlexMarginLeft,
            static (ref readonly _marginLeft, ref b) => 
                b = new FlexMarginLeftBehavior(_marginLeft)
        );

        // FlexMarginRight
        TrySetStruct_Old<float, FlexMarginRightBehavior>(
            entity,
            mutEntity.Value.FlexMarginRight,
            static (ref readonly _marginRight, ref b) => 
                b = new FlexMarginRightBehavior(_marginRight)
        );

        // FlexMarginTop
        TrySetStruct_Old<float, FlexMarginTopBehavior>(
            entity,
            mutEntity.Value.FlexMarginTop,
            static (ref readonly _marginTop, ref b) => 
                b = new FlexMarginTopBehavior(_marginTop)
        );

        // FlexPaddingBottom
        TrySetStruct_Old<float, FlexPaddingBottomBehavior>(
            entity,
            mutEntity.Value.FlexPaddingBottom,
            static (ref readonly _paddingBottom, ref b) => 
                b = new FlexPaddingBottomBehavior(_paddingBottom)
        );

        // FlexPaddingLeft
        TrySetStruct_Old<float, FlexPaddingLeftBehavior>(
            entity,
            mutEntity.Value.FlexPaddingLeft,
            static (ref readonly _paddingLeft, ref b) => 
                b = new FlexPaddingLeftBehavior(_paddingLeft)
        );

        // FlexPaddingRight
        TrySetStruct_Old<float, FlexPaddingRightBehavior>(
            entity,
            mutEntity.Value.FlexPaddingRight,
            static (ref readonly _paddingRight, ref b) => 
                b = new FlexPaddingRightBehavior(_paddingRight)
        );

        // FlexPaddingTop
        TrySetStruct_Old<float, FlexPaddingTopBehavior>(
            entity,
            mutEntity.Value.FlexPaddingTop,
            static (ref readonly _paddingTop, ref b) => 
                b = new FlexPaddingTopBehavior(_paddingTop)
        );

        // FlexPositionBottom - Value
        TrySetStruct_Old<float, FlexPositionBottomBehavior>(
            entity,
            mutEntity.Value.FlexPositionBottom?.Value,
            static (ref readonly _value, ref b) => b = b with
            {
                Value = _value
            }
        );

        // FlexPositionBottom - Percent
        TrySetStruct_Old<bool, FlexPositionBottomBehavior>(
            entity,
            mutEntity.Value.FlexPositionBottom?.Percent,
            static (ref readonly _percent, ref b) => b = b with
            {
                Percent = _percent
            }
        );

        // FlexPositionLeft - Value
        TrySetStruct_Old<float, FlexPositionLeftBehavior>(
            entity,
            mutEntity.Value.FlexPositionLeft?.Value,
            static (ref readonly _value, ref b) => b = b with
            {
                Value = _value
            }
        );

        // FlexPositionLeft - Percent
        TrySetStruct_Old<bool, FlexPositionLeftBehavior>(
            entity,
            mutEntity.Value.FlexPositionLeft?.Percent,
            static (ref readonly _percent, ref b) => b = b with
            {
                Percent = _percent
            }
        );

        // FlexPositionRight - Value
        TrySetStruct_Old<float, FlexPositionRightBehavior>(
            entity,
            mutEntity.Value.FlexPositionRight?.Value,
            static (ref readonly _value, ref b) => b = b with
            {
                Value = _value
            }
        );

        // FlexPositionRight - Percent
        TrySetStruct_Old<bool, FlexPositionRightBehavior>(
            entity,
            mutEntity.Value.FlexPositionRight?.Percent,
            static (ref readonly _percent, ref b) => b = b with
            {
                Percent = _percent
            }
        );

        // FlexPositionTop - Value
        TrySetStruct_Old<float, FlexPositionTopBehavior>(
            entity,
            mutEntity.Value.FlexPositionTop?.Value,
            static (ref readonly _value, ref b) => b = b with
            {
                Value = _value
            }
        );

        // FlexPositionTop - Percent
        TrySetStruct_Old<bool, FlexPositionTopBehavior>(
            entity,
            mutEntity.Value.FlexPositionTop?.Percent,
            static (ref readonly _percent, ref b) => b = b with
            {
                Percent = _percent
            }
        );

        // FlexPositionType
        TrySetStruct_Old<PositionType, FlexPositionTypeBehavior>(
            entity,
            mutEntity.Value.FlexPositionType,
            static (ref readonly _flexPositionType, ref b) =>
                b = new FlexPositionTypeBehavior(_flexPositionType)
        );

        // FlexWidth - Value
        TrySetStruct_Old<float, FlexWidthBehavior>(
            entity,
            mutEntity.Value.FlexWidth?.Value,
            static (ref readonly _value, ref b) => b = b with
            {
                Value = _value
            }
        );

        // FlexWidth - Percent
        TrySetStruct_Old<bool, FlexWidthBehavior>(
            entity,
            mutEntity.Value.FlexWidth?.Percent,
            static (ref readonly _percent, ref b) => b = b with
            {
                Percent = _percent
            }
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
        // Null just means it wont write out back to entity
        if (node == null)
        {
            return;
        }

        if (node is not JsonValue jsonValue)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unable to serialize node to expected type, node was not a value type. Path:'{node?.GetPath()}'"
            ));
            return;
        }

        if (!jsonValue.TryGetValue<TValue>(out var value))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unable to serialize node to expected type. Path:'{node?.GetPath()}' ExpectedType:'{typeof(TValue)} Actual:'{node?.GetValueKind()}'"
            ));

            return;
        }

        entity.SetBehavior(in value, mutate);
    }

    private static void TrySetStruct<TValue, TBehavior>(
        Entity entity,
        JsonNode? node,
        RefInAction<TBehavior, TValue> mutate
    )
        where TBehavior : struct
        where TValue : struct
    {
        // Null just means it wont write out back to entity
        if (node == null)
        {
            return;
        }

        if (node is not JsonValue jsonValue)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unable to serialize node to expected type, node was not a value type. Path:'{node?.GetPath()}'"
            ));
            return;
        }

        if (!jsonValue.TryGetValue<TValue>(out var value))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unable to serialize node to expected type. Path:'{node?.GetPath()}' ExpectedType:'{typeof(TValue)} Actual:'{node?.GetValueKind()}'"
            ));

            return;
        }

        entity.SetBehavior(in value, mutate);
    }


    private static void TrySetRef_Old<TValue, TBehavior>(
        Entity entity,
        TValue? value,
        RefInAction<TBehavior, TValue> mutate
    )
        where TBehavior : struct
        where TValue : class
    {
        if (value == null)
        {
            return;
        }

        entity.SetBehavior(in value, mutate);
    }

    private static void TrySetStruct_Old<TValue, TBehavior>(
        Entity entity,
        TValue? value,
        RefInAction<TBehavior, TValue> mutate
    )
        where TBehavior : struct
        where TValue : struct
    {
        if (!value.HasValue)
        {
            return;
        }

        var helper = value.Value;
        entity.SetBehavior(in helper, mutate);
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

    private static bool TryDeserializeMutEntity(
        JsonObject entityObj,
        [NotNullWhen(true)] 
        out MutEntity? mutEntity
    )
    {
        try
        {
            mutEntity = entityObj.Deserialize<MutEntity>(JsonSerializerOptions.Web);
            return true;
        }
        catch (JsonException ex)
        {
            EventBus<ErrorEvent>.Push(
                new ErrorEvent($"Entity mutation deserialization failed: {ex.Message}")
            );
            mutEntity = null;
            return false;
        }
    }
}
