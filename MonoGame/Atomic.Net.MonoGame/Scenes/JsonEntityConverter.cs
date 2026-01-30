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
    /// Deserializes the entire entity structure at once using Mut Entity structs,
    /// then applies mutations using simple if-checks with null coalescing.
    /// </summary>
    public static void Write(JsonNode jsonEntity, Entity entity)
    {
        if (jsonEntity is not JsonObject entityObj)
        {
            return;
        }

        // Deserialize entire entity structure at once
        if (!TryDeserializeMutEntity(entityObj, out var mutEntity))
        {
            return;
        }

        // Id
        
        TrySetRef<string, IdBehavior>(
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
            foreach (KeyValuePair<string, JsonNode> kvp in mutEntity.Value.Properties)
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
                    propValue = new PropertyValue(strValue);
                }
                else if (jsonValue.TryCoerceFloatValue(out var floatValue))
                {
                    propValue = new PropertyValue(floatValue.Value);
                }
                else if (jsonValue.TryGetValue<bool>(out var boolValue))
                {
                    propValue = new PropertyValue(boolValue);
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
        TrySetStruct<float, TransformBehavior>(
            entity,
            mutEntity.Value.Transform?.Position?.X,
            static (ref readonly _x, ref b) => b = b with
            {
                Position = b.Position with { X = _x }
            }
        );


        // Transform - Position.Y
        TrySetStruct<float, TransformBehavior>(
            entity,
            mutEntity.Value.Transform?.Position?.Y,
            static (ref readonly _y, ref b) => b = b with
            {
                Position = b.Position with { Y = _y }
            }
        );

        // Transform - Position.Z
        if (mutEntity.Value.Transform?.Position?.Z.HasValue ?? false)
        {
            var z = mutEntity.Value.Transform!.Value.Position!.Value.Z!.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in z,
                static (ref readonly _z, ref b) => b = b with
                {
                    Position = b.Position with { Z = _z }
                }
            );
        }

        // Transform - Rotation.X
        if (mutEntity.Value.Transform?.Rotation?.X.HasValue ?? false)
        {
            var x = mutEntity.Value.Transform!.Value.Rotation!.Value.X!.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in x,
                static (ref readonly _x, ref b) => b = b with
                {
                    Rotation = b.Rotation with { X = _x }
                }
            );
        }

        // Transform - Rotation.Y
        if (mutEntity.Value.Transform?.Rotation?.Y.HasValue ?? false)
        {
            var y = mutEntity.Value.Transform!.Value.Rotation!.Value.Y!.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in y,
                static (ref readonly _y, ref b) => b = b with
                {
                    Rotation = b.Rotation with { Y = _y }
                }
            );
        }

        // Transform - Rotation.Z
        if (mutEntity.Value.Transform?.Rotation?.Z.HasValue ?? false)
        {
            var z = mutEntity.Value.Transform!.Value.Rotation!.Value.Z!.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in z,
                static (ref readonly _z, ref b) => b = b with
                {
                    Rotation = b.Rotation with { Z = _z }
                }
            );
        }

        // Transform - Rotation.W
        if (mutEntity.Value.Transform?.Rotation?.W.HasValue ?? false)
        {
            var w = mutEntity.Value.Transform!.Value.Rotation!.Value.W!.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in w,
                static (ref readonly _w, ref b) => b = b with
                {
                    Rotation = b.Rotation with { W = _w }
                }
            );
        }

        // Transform - Scale.X
        if (mutEntity.Value.Transform?.Scale?.X.HasValue ?? false)
        {
            var x = mutEntity.Value.Transform!.Value.Scale!.Value.X!.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in x,
                static (ref readonly _x, ref b) => b = b with
                {
                    Scale = b.Scale with { X = _x }
                }
            );
        }

        // Transform - Scale.Y
        if (mutEntity.Value.Transform?.Scale?.Y.HasValue ?? false)
        {
            var y = mutEntity.Value.Transform!.Value.Scale!.Value.Y!.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in y,
                static (ref readonly _y, ref b) => b = b with
                {
                    Scale = b.Scale with { Y = _y }
                }
            );
        }

        // Transform - Scale.Z
        if (mutEntity.Value.Transform?.Scale?.Z.HasValue ?? false)
        {
            var z = mutEntity.Value.Transform!.Value.Scale!.Value.Z!.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in z,
                static (ref readonly _z, ref b) => b = b with
                {
                    Scale = b.Scale with { Z = _z }
                }
            );
        }

        // Transform - Anchor.X
        if (mutEntity.Value.Transform?.Anchor?.X.HasValue ?? false)
        {
            var x = mutEntity.Value.Transform!.Value.Anchor!.Value.X!.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in x,
                static (ref readonly _x, ref b) => b = b with
                {
                    Anchor = b.Anchor with { X = _x }
                }
            );
        }

        // Transform - Anchor.Y
        if (mutEntity.Value.Transform?.Anchor?.Y.HasValue ?? false)
        {
            var y = mutEntity.Value.Transform!.Value.Anchor!.Value.Y!.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in y,
                static (ref readonly _y, ref b) => b = b with
                {
                    Anchor = b.Anchor with { Y = _y }
                }
            );
        }

        // Transform - Anchor.Z
        if (mutEntity.Value.Transform?.Anchor?.Z.HasValue ?? false)
        {
            var z = mutEntity.Value.Transform!.Value.Anchor!.Value.Z!.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in z,
                static (ref readonly _z, ref b) => b = b with
                {
                    Anchor = b.Anchor with { Z = _z }
                }
            );
        }

        // Parent
        if (mutEntity.Value.Parent != null)
        {
            var parentSelector = mutEntity.Value.Parent;
            entity.SetBehavior<ParentBehavior, EntitySelector>(
                in parentSelector,
                static (ref readonly _selector, ref b) => b = new ParentBehavior(_selector)
            );
        }

        // FlexAlignItems
        if (mutEntity.Value.FlexAlignItems != null)
        {
            var alignItems = mutEntity.Value.FlexAlignItems.Value;
            entity.SetBehavior<FlexAlignItemsBehavior, Align>(
                in alignItems,
                static (ref readonly _alignItems, ref b) =>
                    b = new FlexAlignItemsBehavior(_alignItems)
            );
        }

        // FlexAlignSelf
        if (mutEntity.Value.FlexAlignSelf != null)
        {
            var alignSelf = mutEntity.Value.FlexAlignSelf.Value;
            entity.SetBehavior<FlexAlignSelfBehavior, Align>(
                in alignSelf,
                static (ref readonly _alignSelf, ref b) =>
                    b = new FlexAlignSelfBehavior(_alignSelf)
            );
        }

        // FlexBorderBottom
        if (mutEntity.Value.FlexBorderBottom.HasValue)
        {
            var borderBottom = mutEntity.Value.FlexBorderBottom.Value;
            entity.SetBehavior<FlexBorderBottomBehavior, float>(
                in borderBottom,
                static (ref readonly _borderBottom, ref b) =>
                        b = new FlexBorderBottomBehavior(_borderBottom)
            );
        }

        // FlexBorderLeft
        if (mutEntity.Value.FlexBorderLeft.HasValue)
        {
            var borderLeft = mutEntity.Value.FlexBorderLeft.Value;
            entity.SetBehavior<FlexBorderLeftBehavior, float>(
                in borderLeft,
                static (ref readonly _borderLeft, ref b) =>
                        b = new FlexBorderLeftBehavior(_borderLeft)
            );
        }

        // FlexBorderRight
        if (mutEntity.Value.FlexBorderRight.HasValue)
        {
            var borderRight = mutEntity.Value.FlexBorderRight.Value;
            entity.SetBehavior<FlexBorderRightBehavior, float>(
                in borderRight,
                static (ref readonly _borderRight, ref b) =>
                        b = new FlexBorderRightBehavior(_borderRight)
            );
        }

        // FlexBorderTop
        if (mutEntity.Value.FlexBorderTop.HasValue)
        {
            var borderTop = mutEntity.Value.FlexBorderTop.Value;
            entity.SetBehavior<FlexBorderTopBehavior, float>(
                in borderTop,
                static (ref readonly _borderTop, ref b) =>
                        b = new FlexBorderTopBehavior(_borderTop)
            );
        }

        // FlexDirection
        if (mutEntity.Value.FlexDirection != null)
        {
            var direction = mutEntity.Value.FlexDirection.Value;
            entity.SetBehavior<FlexDirectionBehavior, FlexDirection>(
                in direction,
                static (ref readonly _direction, ref b) =>
                    b = new FlexDirectionBehavior(_direction)
            );
        }

        // FlexGrow
        if (mutEntity.Value.FlexGrow.HasValue)
        {
            var grow = mutEntity.Value.FlexGrow.Value;
            entity.SetBehavior<FlexGrowBehavior, float>(
                in grow,
                static (ref readonly _grow, ref b) =>
                        b = new FlexGrowBehavior(_grow)
            );
        }

        // FlexWrap
        if (mutEntity.Value.FlexWrap != null)
        {
            var wrap = mutEntity.Value.FlexWrap.Value;
            entity.SetBehavior<FlexWrapBehavior, Wrap>(
                in wrap,
                static (ref readonly _wrap, ref b) =>
                    b = new FlexWrapBehavior(_wrap)
            );
        }

        // FlexZOverride
        if (mutEntity.Value.FlexZOverride.HasValue)
        {
            var zOverride = mutEntity.Value.FlexZOverride.Value;
            entity.SetBehavior<FlexZOverride, int>(
                in zOverride,
                static (ref readonly _zOverride, ref b) =>
                        b = new FlexZOverride(_zOverride)
            );
        }

        // FlexHeight - Value
        if (mutEntity.Value.FlexHeight?.Value.HasValue ?? false)
        {
            var value = mutEntity.Value.FlexHeight.Value.Value.Value;
            entity.SetBehavior<FlexHeightBehavior, float>(
                in value,
                static (ref readonly _value, ref b) => b = b with
                {
                    Value = _value
                }
            );
        }

        // FlexHeight - Percent
        if (mutEntity.Value.FlexHeight?.Percent.HasValue ?? false)
        {
            var percent = mutEntity.Value.FlexHeight.Value.Percent.Value;
            entity.SetBehavior<FlexHeightBehavior, bool>(
                in percent,
                static (ref readonly _percent, ref b) => b = b with
                {
                    Percent = _percent
                }
            );
        }

        // FlexJustifyContent
        if (mutEntity.Value.FlexJustifyContent != null)
        {
            var justify = mutEntity.Value.FlexJustifyContent.Value;
            entity.SetBehavior<FlexJustifyContentBehavior, Justify>(
                in justify,
                static (ref readonly _justify, ref b) =>
                    b = new FlexJustifyContentBehavior(_justify)
            );
        }

        // FlexMarginBottom
        if (mutEntity.Value.FlexMarginBottom.HasValue)
        {
            var marginBottom = mutEntity.Value.FlexMarginBottom.Value;
            entity.SetBehavior<FlexMarginBottomBehavior, float>(
                in marginBottom,
                static (ref readonly _marginBottom, ref b) => 
                    b = new FlexMarginBottomBehavior(_marginBottom)
            );
        }

        // FlexMarginLeft
        if (mutEntity.Value.FlexMarginLeft.HasValue)
        {
            var marginLeft = mutEntity.Value.FlexMarginLeft.Value;
            entity.SetBehavior<FlexMarginLeftBehavior, float>(
                in marginLeft,
                static (ref readonly _marginLeft, ref b) => 
                    b = new FlexMarginLeftBehavior(_marginLeft)
            );
        }

        // FlexMarginRight
        if (mutEntity.Value.FlexMarginRight.HasValue)
        {
            var marginRight = mutEntity.Value.FlexMarginRight.Value;
            entity.SetBehavior<FlexMarginRightBehavior, float>(
                in marginRight,
                static (ref readonly _marginRight, ref b) => 
                    b = new FlexMarginRightBehavior(_marginRight)
            );
        }

        // FlexMarginTop
        if (mutEntity.Value.FlexMarginTop.HasValue)
        {
            var marginTop = mutEntity.Value.FlexMarginTop.Value;
            entity.SetBehavior<FlexMarginTopBehavior, float>(
                in marginTop,
                static (ref readonly _marginTop, ref b) => 
                    b = new FlexMarginTopBehavior(_marginTop)
            );
        }

        // FlexPaddingBottom
        if (mutEntity.Value.FlexPaddingBottom.HasValue)
        {
            var paddingBottom = mutEntity.Value.FlexPaddingBottom.Value;
            entity.SetBehavior<FlexPaddingBottomBehavior, float>(
                in paddingBottom,
                static (ref readonly _paddingBottom, ref b) => 
                    b = new FlexPaddingBottomBehavior(_paddingBottom)
            );
        }

        // FlexPaddingLeft
        if (mutEntity.Value.FlexPaddingLeft.HasValue)
        {
            var paddingLeft = mutEntity.Value.FlexPaddingLeft.Value;
            entity.SetBehavior<FlexPaddingLeftBehavior, float>(
                in paddingLeft,
                static (ref readonly _paddingLeft, ref b) => 
                    b = new FlexPaddingLeftBehavior(_paddingLeft)
            );
        }

        // FlexPaddingRight
        if (mutEntity.Value.FlexPaddingRight.HasValue)
        {
            var paddingRight = mutEntity.Value.FlexPaddingRight.Value;
            entity.SetBehavior<FlexPaddingRightBehavior, float>(
                in paddingRight,
                static (ref readonly _paddingRight, ref b) => 
                    b = new FlexPaddingRightBehavior(_paddingRight)
            );
        }

        // FlexPaddingTop
        if (mutEntity.Value.FlexPaddingTop.HasValue)
        {
            var paddingTop = mutEntity.Value.FlexPaddingTop.Value;
            entity.SetBehavior<FlexPaddingTopBehavior, float>(
                in paddingTop,
                static (ref readonly _paddingTop, ref b) => 
                    b = new FlexPaddingTopBehavior(_paddingTop)
            );
        }

        // FlexPositionBottom - Value
        if (mutEntity.Value.FlexPositionBottom?.Value.HasValue ?? false)
        {
            var value = mutEntity.Value.FlexPositionBottom.Value.Value.Value;
            entity.SetBehavior<FlexPositionBottomBehavior, float>(
                in value,
                static (ref readonly _value, ref b) => b = b with
                {
                    Value = _value
                }
            );
        }

        // FlexPositionBottom - Percent
        if (mutEntity.Value.FlexPositionBottom?.Percent.HasValue ?? false)
        {
            var percent = mutEntity.Value.FlexPositionBottom.Value.Percent.Value;
            entity.SetBehavior<FlexPositionBottomBehavior, bool>(
                in percent,
                static (ref readonly _percent, ref b) => b = b with
                {
                    Percent = _percent
                }
            );
        }

        // FlexPositionLeft - Value
        if (mutEntity.Value.FlexPositionLeft?.Value.HasValue ?? false)
        {
            var value = mutEntity.Value.FlexPositionLeft.Value.Value.Value;
            entity.SetBehavior<FlexPositionLeftBehavior, float>(
                in value,
                static (ref readonly _value, ref b) => b = b with
                {
                    Value = _value
                }
            );
        }

        // FlexPositionLeft - Percent
        if (mutEntity.Value.FlexPositionLeft?.Percent.HasValue ?? false)
        {
            var percent = mutEntity.Value.FlexPositionLeft.Value.Percent.Value;
            entity.SetBehavior<FlexPositionLeftBehavior, bool>(
                in percent,
                static (ref readonly _percent, ref b) => b = b with
                {
                    Percent = _percent
                }
            );
        }

        // FlexPositionRight - Value
        if (mutEntity.Value.FlexPositionRight?.Value.HasValue ?? false)
        {
            var value = mutEntity.Value.FlexPositionRight.Value.Value.Value;
            entity.SetBehavior<FlexPositionRightBehavior, float>(
                in value,
                static (ref readonly _value, ref b) => b = b with
                {
                    Value = _value
                }
            );
        }

        // FlexPositionRight - Percent
        if (mutEntity.Value.FlexPositionRight?.Percent.HasValue ?? false)
        {
            var percent = mutEntity.Value.FlexPositionRight.Value.Percent.Value;
            entity.SetBehavior<FlexPositionRightBehavior, bool>(
                in percent,
                static (ref readonly _percent, ref b) => b = b with
                {
                    Percent = _percent
                }
            );
        }

        // FlexPositionTop - Value
        if (mutEntity.Value.FlexPositionTop?.Value.HasValue ?? false)
        {
            var value = mutEntity.Value.FlexPositionTop.Value.Value.Value;
            entity.SetBehavior<FlexPositionTopBehavior, float>(
                in value,
                static (ref readonly _value, ref b) => b = b with
                {
                    Value = _value
                }
            );
        }

        // FlexPositionTop - Percent
        if (mutEntity.Value.FlexPositionTop?.Percent.HasValue ?? false)
        {
            var percent = mutEntity.Value.FlexPositionTop.Value.Percent.Value;
            entity.SetBehavior<FlexPositionTopBehavior, bool>(
                in percent,
                static (ref readonly _percent, ref b) => b = b with
                {
                    Percent = _percent
                }
            );
        }

        // FlexPositionType
        if (mutEntity.Value.FlexPositionType != null)
        {
            var flexPositionType = mutEntity.Value.FlexPositionType.Value;
            entity.SetBehavior<FlexPositionTypeBehavior, PositionType>(
                in flexPositionType,
                static (ref readonly _flexPositionType, ref b) =>
                    b = new FlexPositionTypeBehavior(_flexPositionType)
            );
        }

        // FlexWidth - Value
        if (mutEntity.Value.FlexWidth?.Value.HasValue ?? false)
        {
            var value = mutEntity.Value.FlexWidth.Value.Value.Value;
            entity.SetBehavior<FlexWidthBehavior, float>(
                in value,
                static (ref readonly _value, ref b) => b = b with
                {
                    Value = _value
                }
            );
        }

        // FlexWidth - Percent
        if (mutEntity.Value.FlexWidth?.Percent.HasValue ?? false)
        {
            var percent = mutEntity.Value.FlexWidth.Value.Percent.Value;
            entity.SetBehavior<FlexWidthBehavior, bool>(
                in percent,
                static (ref readonly _percent, ref b) => b = b with
                {
                    Percent = _percent
                }
            );
        }
    }


    private static void TrySetRef<TValue, TBehavior>(
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

    private static void TrySetStruct<TValue, TBehavior>(
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
