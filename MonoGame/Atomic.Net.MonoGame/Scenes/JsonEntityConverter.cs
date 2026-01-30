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

        SerializeFlexBehaviors(entity, entityObj);

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
        if (mutEntity.Id != null)
        {
            var newId = mutEntity.Id;
            entity.SetBehavior<IdBehavior, string>(
                in newId,
                static (ref readonly string _newId, ref IdBehavior b) => b = new IdBehavior(_newId)
            );
        }

        // Tags
        if (mutEntity.Tags != null)
        {
            entity.SetBehavior<TagsBehavior>(static (ref b) => b = b with { Tags = b.Tags.Clear() });
            
            foreach (var tagNode in mutEntity.Tags)
            {
                if (tagNode == null)
                {
                    EventBus<ErrorEvent>.Push(new ErrorEvent("Tags mutation failed: tag cannot be null"));
                    continue;
                }

                if (tagNode.GetValueKind() != System.Text.Json.JsonValueKind.String)
                {
                    EventBus<ErrorEvent>.Push(new ErrorEvent($"Tags mutation failed: expected string, got {tagNode.GetValueKind()}"));
                    continue;
                }

                var tag = tagNode.GetValue<string>();
                entity.SetBehavior<TagsBehavior, string>(
                    in tag,
                    static (ref readonly _tag, ref b) => b = b with { Tags = b.Tags.With(_tag) }
                );
            }
        }

        // Properties
        if (mutEntity.Properties != null)
        {
            foreach (KeyValuePair<string, JsonNode> kvp in mutEntity.Properties)
            {
                var key = kvp.Key;
                var valueNode = kvp.Value;
                
                if (valueNode == null)
                {
                    continue;
                }

                PropertyValue? propValue = null;

                if (valueNode is JsonValue jsonValue)
                {
                    if (jsonValue.TryGetValue<string>(out var strValue))
                    {
                        propValue = new PropertyValue(strValue);
                    }
                    else if (jsonValue.TryGetFloatValue(out var floatValue))
                    {
                        propValue = new PropertyValue(floatValue);
                    }
                    else if (jsonValue.TryGetValue<bool>(out var boolValue))
                    {
                        propValue = new PropertyValue(boolValue);
                    }
                }

                if (propValue.HasValue)
                {
                    var data = (key, propValue.Value);
                    entity.SetBehavior<PropertiesBehavior, (string Key, PropertyValue Value)>(
                        in data,
                        static (ref readonly (string Key, PropertyValue Value) _data, ref PropertiesBehavior b) =>
                            b = b with { Properties = b.Properties.With(_data.Key, _data.Value) }
                    );
                }
            }
        }

        // Transform - Position.X
        if (mutEntity.Transform?.Position?.X.HasValue ?? false)
        {
            var x = mutEntity.Transform.Value.Position.Value.X.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in x,
                static (ref readonly float _x, ref TransformBehavior b) => b = b with { Position = b.Position with { X = _x } }
            );
        }
        
        // Transform - Position.Y
        if (mutEntity.Transform?.Position?.Y.HasValue ?? false)
        {
            var y = mutEntity.Transform.Value.Position.Value.Y.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in y,
                static (ref readonly float _y, ref TransformBehavior b) => b = b with { Position = b.Position with { Y = _y } }
            );
        }
        
        // Transform - Position.Z
        if (mutEntity.Transform?.Position?.Z.HasValue ?? false)
        {
            var z = mutEntity.Transform.Value.Position.Value.Z.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in z,
                static (ref readonly float _z, ref TransformBehavior b) => b = b with { Position = b.Position with { Z = _z } }
            );
        }

        // Transform - Rotation.X
        if (mutEntity.Transform?.Rotation?.X.HasValue ?? false)
        {
            var x = mutEntity.Transform.Value.Rotation.Value.X.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in x,
                static (ref readonly float _x, ref TransformBehavior b) => b = b with { Rotation = b.Rotation with { X = _x } }
            );
        }
        
        // Transform - Rotation.Y
        if (mutEntity.Transform?.Rotation?.Y.HasValue ?? false)
        {
            var y = mutEntity.Transform.Value.Rotation.Value.Y.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in y,
                static (ref readonly float _y, ref TransformBehavior b) => b = b with { Rotation = b.Rotation with { Y = _y } }
            );
        }
        
        // Transform - Rotation.Z
        if (mutEntity.Transform?.Rotation?.Z.HasValue ?? false)
        {
            var z = mutEntity.Transform.Value.Rotation.Value.Z.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in z,
                static (ref readonly float _z, ref TransformBehavior b) => b = b with { Rotation = b.Rotation with { Z = _z } }
            );
        }
        
        // Transform - Rotation.W
        if (mutEntity.Transform?.Rotation?.W.HasValue ?? false)
        {
            var w = mutEntity.Transform.Value.Rotation.Value.W.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in w,
                static (ref readonly float _w, ref TransformBehavior b) => b = b with { Rotation = b.Rotation with { W = _w } }
            );
        }

        // Transform - Scale.X
        if (mutEntity.Transform?.Scale?.X.HasValue ?? false)
        {
            var x = mutEntity.Transform.Value.Scale.Value.X.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in x,
                static (ref readonly float _x, ref TransformBehavior b) => b = b with { Scale = b.Scale with { X = _x } }
            );
        }
        
        // Transform - Scale.Y
        if (mutEntity.Transform?.Scale?.Y.HasValue ?? false)
        {
            var y = mutEntity.Transform.Value.Scale.Value.Y.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in y,
                static (ref readonly float _y, ref TransformBehavior b) => b = b with { Scale = b.Scale with { Y = _y } }
            );
        }
        
        // Transform - Scale.Z
        if (mutEntity.Transform?.Scale?.Z.HasValue ?? false)
        {
            var z = mutEntity.Transform.Value.Scale.Value.Z.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in z,
                static (ref readonly float _z, ref TransformBehavior b) => b = b with { Scale = b.Scale with { Z = _z } }
            );
        }

        // Transform - Anchor.X
        if (mutEntity.Transform?.Anchor?.X.HasValue ?? false)
        {
            var x = mutEntity.Transform.Value.Anchor.Value.X.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in x,
                static (ref readonly float _x, ref TransformBehavior b) => b = b with { Anchor = b.Anchor with { X = _x } }
            );
        }
        
        // Transform - Anchor.Y
        if (mutEntity.Transform?.Anchor?.Y.HasValue ?? false)
        {
            var y = mutEntity.Transform.Value.Anchor.Value.Y.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in y,
                static (ref readonly float _y, ref TransformBehavior b) => b = b with { Anchor = b.Anchor with { Y = _y } }
            );
        }
        
        // Transform - Anchor.Z
        if (mutEntity.Transform?.Anchor?.Z.HasValue ?? false)
        {
            var z = mutEntity.Transform.Value.Anchor.Value.Z.Value;
            entity.SetBehavior<TransformBehavior, float>(
                in z,
                static (ref readonly float _z, ref TransformBehavior b) => b = b with { Anchor = b.Anchor with { Z = _z } }
            );
        }

        // Parent
        if (mutEntity.Parent != null)
        {
            if (!SelectorRegistry.Instance.TryParse(mutEntity.Parent, out var parentSelector))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent($"Parent mutation failed: invalid selector '{mutEntity.Parent}'"));
            }
            else
            {
                entity.SetBehavior<ParentBehavior, EntitySelector>(
                    in parentSelector,
                    static (ref readonly EntitySelector _selector, ref ParentBehavior b) => b = new ParentBehavior(_selector)
                );
            }
        }

        // FlexAlignItems
        if (mutEntity.FlexAlignItems != null)
        {
            if (!Enum.TryParse<Align>(mutEntity.FlexAlignItems, true, out var alignItems))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent($"FlexAlignItems mutation failed: expected valid Align enum value"));
            }
            else
            {
                entity.SetBehavior<FlexAlignItemsBehavior, Align>(
                    in alignItems,
                    static (ref readonly Align val, ref FlexAlignItemsBehavior b) => b = new FlexAlignItemsBehavior(val)
                );
            }
        }

        // FlexAlignSelf
        if (mutEntity.FlexAlignSelf != null)
        {
            if (!Enum.TryParse<Align>(mutEntity.FlexAlignSelf, true, out var alignSelf))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent($"FlexAlignSelf mutation failed: expected valid Align enum value"));
            }
            else
            {
                entity.SetBehavior<FlexAlignSelfBehavior, Align>(
                    in alignSelf,
                    static (ref readonly Align val, ref FlexAlignSelfBehavior b) => b = new FlexAlignSelfBehavior(val)
                );
            }
        }

        // FlexBorderBottom
        if (mutEntity.FlexBorderBottom.HasValue)
        {
            var borderBottom = mutEntity.FlexBorderBottom.Value;
            entity.SetBehavior<FlexBorderBottomBehavior, float>(
                in borderBottom,
                static (ref readonly float val, ref FlexBorderBottomBehavior b) => b = new FlexBorderBottomBehavior(val)
            );
        }

        // FlexBorderLeft
        if (mutEntity.FlexBorderLeft.HasValue)
        {
            var borderLeft = mutEntity.FlexBorderLeft.Value;
            entity.SetBehavior<FlexBorderLeftBehavior, float>(
                in borderLeft,
                static (ref readonly float val, ref FlexBorderLeftBehavior b) => b = new FlexBorderLeftBehavior(val)
            );
        }

        // FlexBorderRight
        if (mutEntity.FlexBorderRight.HasValue)
        {
            var borderRight = mutEntity.FlexBorderRight.Value;
            entity.SetBehavior<FlexBorderRightBehavior, float>(
                in borderRight,
                static (ref readonly float val, ref FlexBorderRightBehavior b) => b = new FlexBorderRightBehavior(val)
            );
        }

        // FlexBorderTop
        if (mutEntity.FlexBorderTop.HasValue)
        {
            var borderTop = mutEntity.FlexBorderTop.Value;
            entity.SetBehavior<FlexBorderTopBehavior, float>(
                in borderTop,
                static (ref readonly float val, ref FlexBorderTopBehavior b) => b = new FlexBorderTopBehavior(val)
            );
        }

        // FlexDirection
        if (mutEntity.FlexDirection != null)
        {
            if (!Enum.TryParse<FlexDirection>(mutEntity.FlexDirection, true, out var direction))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent($"FlexDirection mutation failed: expected valid FlexDirection enum value"));
            }
            else
            {
                entity.SetBehavior<FlexDirectionBehavior, FlexDirection>(
                    in direction,
                    static (ref readonly FlexDirection val, ref FlexDirectionBehavior b) => b = new FlexDirectionBehavior(val)
                );
            }
        }

        // FlexGrow
        if (mutEntity.FlexGrow.HasValue)
        {
            var grow = mutEntity.FlexGrow.Value;
            entity.SetBehavior<FlexGrowBehavior, float>(
                in grow,
                static (ref readonly float val, ref FlexGrowBehavior b) => b = new FlexGrowBehavior(val)
            );
        }

        // FlexWrap
        if (mutEntity.FlexWrap != null)
        {
            if (!Enum.TryParse<Wrap>(mutEntity.FlexWrap, true, out var wrap))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent($"FlexWrap mutation failed: expected valid Wrap enum value"));
            }
            else
            {
                entity.SetBehavior<FlexWrapBehavior, Wrap>(
                    in wrap,
                    static (ref readonly Wrap val, ref FlexWrapBehavior b) => b = new FlexWrapBehavior(val)
                );
            }
        }

        // FlexZOverride
        if (mutEntity.FlexZOverride.HasValue)
        {
            var zOverride = mutEntity.FlexZOverride.Value;
            entity.SetBehavior<FlexZOverride, int>(
                in zOverride,
                static (ref readonly int val, ref FlexZOverride b) => b = new FlexZOverride(val)
            );
        }

        // FlexHeight
        if (mutEntity.FlexHeight.HasValue)
        {
            var height = mutEntity.FlexHeight.Value;
            var percent = mutEntity.FlexHeightPercent ?? false;
            var data = (height, percent);
            entity.SetBehavior<FlexHeightBehavior, (float Height, bool Percent)>(
                in data,
                static (ref readonly (float Height, bool Percent) val, ref FlexHeightBehavior b) => b = new FlexHeightBehavior(val.Height, val.Percent)
            );
        }

        // FlexJustifyContent
        if (mutEntity.FlexJustifyContent != null)
        {
            if (!Enum.TryParse<Justify>(mutEntity.FlexJustifyContent, true, out var justify))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent($"FlexJustifyContent mutation failed: expected valid Justify enum value"));
            }
            else
            {
                entity.SetBehavior<FlexJustifyContentBehavior, Justify>(
                    in justify,
                    static (ref readonly Justify val, ref FlexJustifyContentBehavior b) => b = new FlexJustifyContentBehavior(val)
                );
            }
        }

        // FlexMarginBottom
        if (mutEntity.FlexMarginBottom.HasValue)
        {
            var marginBottom = mutEntity.FlexMarginBottom.Value;
            entity.SetBehavior<FlexMarginBottomBehavior, float>(
                in marginBottom,
                static (ref readonly float val, ref FlexMarginBottomBehavior b) => b = new FlexMarginBottomBehavior(val)
            );
        }

        // FlexMarginLeft
        if (mutEntity.FlexMarginLeft.HasValue)
        {
            var marginLeft = mutEntity.FlexMarginLeft.Value;
            entity.SetBehavior<FlexMarginLeftBehavior, float>(
                in marginLeft,
                static (ref readonly float val, ref FlexMarginLeftBehavior b) => b = new FlexMarginLeftBehavior(val)
            );
        }

        // FlexMarginRight
        if (mutEntity.FlexMarginRight.HasValue)
        {
            var marginRight = mutEntity.FlexMarginRight.Value;
            entity.SetBehavior<FlexMarginRightBehavior, float>(
                in marginRight,
                static (ref readonly float val, ref FlexMarginRightBehavior b) => b = new FlexMarginRightBehavior(val)
            );
        }

        // FlexMarginTop
        if (mutEntity.FlexMarginTop.HasValue)
        {
            var marginTop = mutEntity.FlexMarginTop.Value;
            entity.SetBehavior<FlexMarginTopBehavior, float>(
                in marginTop,
                static (ref readonly float val, ref FlexMarginTopBehavior b) => b = new FlexMarginTopBehavior(val)
            );
        }

        // FlexPaddingBottom
        if (mutEntity.FlexPaddingBottom.HasValue)
        {
            var paddingBottom = mutEntity.FlexPaddingBottom.Value;
            entity.SetBehavior<FlexPaddingBottomBehavior, float>(
                in paddingBottom,
                static (ref readonly float val, ref FlexPaddingBottomBehavior b) => b = new FlexPaddingBottomBehavior(val)
            );
        }

        // FlexPaddingLeft
        if (mutEntity.FlexPaddingLeft.HasValue)
        {
            var paddingLeft = mutEntity.FlexPaddingLeft.Value;
            entity.SetBehavior<FlexPaddingLeftBehavior, float>(
                in paddingLeft,
                static (ref readonly float val, ref FlexPaddingLeftBehavior b) => b = new FlexPaddingLeftBehavior(val)
            );
        }

        // FlexPaddingRight
        if (mutEntity.FlexPaddingRight.HasValue)
        {
            var paddingRight = mutEntity.FlexPaddingRight.Value;
            entity.SetBehavior<FlexPaddingRightBehavior, float>(
                in paddingRight,
                static (ref readonly float val, ref FlexPaddingRightBehavior b) => b = new FlexPaddingRightBehavior(val)
            );
        }

        // FlexPaddingTop
        if (mutEntity.FlexPaddingTop.HasValue)
        {
            var paddingTop = mutEntity.FlexPaddingTop.Value;
            entity.SetBehavior<FlexPaddingTopBehavior, float>(
                in paddingTop,
                static (ref readonly float val, ref FlexPaddingTopBehavior b) => b = new FlexPaddingTopBehavior(val)
            );
        }

        // FlexPositionBottom
        if (mutEntity.FlexPositionBottom.HasValue)
        {
            var positionBottom = mutEntity.FlexPositionBottom.Value;
            var percent = mutEntity.FlexPositionBottomPercent ?? false;
            var data = (positionBottom, percent);
            entity.SetBehavior<FlexPositionBottomBehavior, (float Value, bool Percent)>(
                in data,
                static (ref readonly (float Value, bool Percent) val, ref FlexPositionBottomBehavior b) => b = new FlexPositionBottomBehavior(val.Value, val.Percent)
            );
        }

        // FlexPositionLeft
        if (mutEntity.FlexPositionLeft.HasValue)
        {
            var positionLeft = mutEntity.FlexPositionLeft.Value;
            var percent = mutEntity.FlexPositionLeftPercent ?? false;
            var data = (positionLeft, percent);
            entity.SetBehavior<FlexPositionLeftBehavior, (float Value, bool Percent)>(
                in data,
                static (ref readonly (float Value, bool Percent) val, ref FlexPositionLeftBehavior b) => b = new FlexPositionLeftBehavior(val.Value, val.Percent)
            );
        }

        // FlexPositionRight
        if (mutEntity.FlexPositionRight.HasValue)
        {
            var positionRight = mutEntity.FlexPositionRight.Value;
            var percent = mutEntity.FlexPositionRightPercent ?? false;
            var data = (positionRight, percent);
            entity.SetBehavior<FlexPositionRightBehavior, (float Value, bool Percent)>(
                in data,
                static (ref readonly (float Value, bool Percent) val, ref FlexPositionRightBehavior b) => b = new FlexPositionRightBehavior(val.Value, val.Percent)
            );
        }

        // FlexPositionTop
        if (mutEntity.FlexPositionTop.HasValue)
        {
            var positionTop = mutEntity.FlexPositionTop.Value;
            var percent = mutEntity.FlexPositionTopPercent ?? false;
            var data = (positionTop, percent);
            entity.SetBehavior<FlexPositionTopBehavior, (float Value, bool Percent)>(
                in data,
                static (ref readonly (float Value, bool Percent) val, ref FlexPositionTopBehavior b) => b = new FlexPositionTopBehavior(val.Value, val.Percent)
            );
        }

        // FlexPositionType
        if (mutEntity.FlexPositionType != null)
        {
            if (!Enum.TryParse<PositionType>(mutEntity.FlexPositionType, true, out var positionType))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent($"FlexPositionType mutation failed: expected valid PositionType enum value"));
            }
            else
            {
                entity.SetBehavior<FlexPositionTypeBehavior, PositionType>(
                    in positionType,
                    static (ref readonly PositionType val, ref FlexPositionTypeBehavior b) => b = new FlexPositionTypeBehavior(val)
                );
            }
        }

        // FlexWidth
        if (mutEntity.FlexWidth.HasValue)
        {
            var width = mutEntity.FlexWidth.Value;
            var percent = mutEntity.FlexWidthPercent ?? false;
            var data = (width, percent);
            entity.SetBehavior<FlexWidthBehavior, (float Width, bool Percent)>(
                in data,
                static (ref readonly (float Width, bool Percent) val, ref FlexWidthBehavior b) => b = new FlexWidthBehavior(val.Width, val.Percent)
            );
        }
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

    private static void SerializeFlexBehaviors(Entity entity, JsonObject entityObj)
    {
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
            entityObj["flexHeight"] = height.Value.Value;
            entityObj["flexHeightPercent"] = height.Value.Percent;
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
            entityObj["flexPositionBottom"] = positionBottom.Value.Value;
            entityObj["flexPositionBottomPercent"] = positionBottom.Value.Percent;
        }

        if (entity.TryGetBehavior<FlexPositionLeftBehavior>(out var positionLeft))
        {
            entityObj["flexPositionLeft"] = positionLeft.Value.Value;
            entityObj["flexPositionLeftPercent"] = positionLeft.Value.Percent;
        }

        if (entity.TryGetBehavior<FlexPositionRightBehavior>(out var positionRight))
        {
            entityObj["flexPositionRight"] = positionRight.Value.Value;
            entityObj["flexPositionRightPercent"] = positionRight.Value.Percent;
        }

        if (entity.TryGetBehavior<FlexPositionTopBehavior>(out var positionTop))
        {
            entityObj["flexPositionTop"] = positionTop.Value.Value;
            entityObj["flexPositionTopPercent"] = positionTop.Value.Percent;
        }

        if (entity.TryGetBehavior<FlexPositionTypeBehavior>(out var positionType))
        {
            entityObj["flexPositionType"] = positionType.Value.Value.ToString();
        }

        if (entity.TryGetBehavior<FlexWidthBehavior>(out var width))
        {
            entityObj["flexWidth"] = width.Value.Value;
            entityObj["flexWidthPercent"] = width.Value.Percent;
        }
    }

    private static bool TryDeserializeMutEntity(JsonObject entityObj, out MutEntity mutEntity)
    {
        try
        {
            mutEntity = entityObj.Deserialize<MutEntity>(JsonSerializerOptions.Web);
            return true;
        }
        catch (JsonException ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Entity mutation deserialization failed: {ex.Message}"));
            mutEntity = default;
            return false;
        }
    }
}
