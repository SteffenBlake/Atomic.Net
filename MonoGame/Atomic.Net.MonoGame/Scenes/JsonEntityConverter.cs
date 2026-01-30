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

using Vector3 = Microsoft.Xna.Framework.Vector3;
using Quaternion = Microsoft.Xna.Framework.Quaternion;

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
    /// </summary>
    public static void Write(JsonNode jsonEntity, Entity entity)
    {
        if (jsonEntity is not JsonObject entityObj)
        {
            return;
        }

        // Write properties
        if (entityObj.TryGetPropertyValue("properties", out var propertiesNode) && propertiesNode is JsonObject propertiesObj)
        {
            WriteProperties(entity, propertiesObj);
        }

        // Write ID
        if (entityObj.TryGetPropertyValue("id", out var idNode) && idNode is JsonValue idValue)
        {
            WriteId(entity, idValue);
        }

        // Write tags
        if (entityObj.TryGetPropertyValue("tags", out var tagsNode) && tagsNode is JsonArray tagsArray)
        {
            WriteTags(entity, tagsArray);
        }

        // Write parent
        if (entityObj.TryGetPropertyValue("parent", out var parentNode) && parentNode is JsonValue parentValue)
        {
            WriteParent(entity, parentValue);
        }

        // Write transform
        if (entityObj.TryGetPropertyValue("transform", out var transformNode) && transformNode is JsonObject transformObj)
        {
            WriteTransform(entity, transformObj);
        }

        // Write flex behaviors
        WriteFlexBehaviors(entity, entityObj);
    }

    private static void WriteProperties(Entity entity, JsonObject propertiesObj)
    {
        foreach (var (key, value) in propertiesObj)
        {
            if (value is null)
            {
                continue;
            }

            if (!TryConvertToPropertyValue(value, out var propertyValue))
            {
                continue;
            }

            var setter = (key, propertyValue.Value);
            entity.SetBehavior<PropertiesBehavior, (string Key, PropertyValue Value)>(
                in setter,
                static (ref readonly (string Key, PropertyValue Value) _setter, ref PropertiesBehavior b) => b = b with { 
                    Properties = b.Properties.With(_setter.Key, _setter.Value) 
                }
            );
        }
    }

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
        
        if (jsonValue.TryGetValue<double>(out var doubleVal))
        {
            propertyValue = (float)doubleVal;
            return true;
        }
        
        if (jsonValue.TryGetValue<int>(out var intVal))
        {
            propertyValue = (float)intVal;
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

    private static void WriteId(Entity entity, JsonValue idValue)
    {
        if (!idValue.TryGetValue<string>(out var newId))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"ID mutation failed: expected string value, got {idValue.GetValueKind()}"
            ));
            return;
        }

        entity.SetBehavior<IdBehavior, string>(
            in newId,
            static (ref readonly _newId, ref b) => b = new IdBehavior(_newId)
        );
    }

    private static void WriteTags(Entity entity, JsonArray tagsArray)
    {
        // First clear existing tags
        entity.SetBehavior<TagsBehavior>(
            static (ref b) => b = b with { Tags = b.Tags.Clear() }
        );

        // Then add each valid tag
        var hasInvalidTag = false;
        foreach (var tagNode in tagsArray)
        {
            if (tagNode is JsonValue tagValue && tagValue.TryGetValue<string>(out var tag))
            {
                entity.SetBehavior<TagsBehavior, string>(
                    in tag,
                    static (ref readonly _tag, ref b) => b = b with { Tags = b.Tags.With(_tag) }
                );
            }
            else
            {
                hasInvalidTag = true;
            }
        }

        if (hasInvalidTag)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Tags mutation failed: all tags must be string values"
            ));
        }
    }

    private static void WriteParent(Entity entity, JsonValue parentValue)
    {
        if (!parentValue.TryGetValue<string>(out var parentSelector))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Parent mutation failed: expected string selector value"
            ));
            return;
        }

        if (!SelectorRegistry.Instance.TryParse(parentSelector, out var selector))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Parent mutation failed: invalid selector syntax '{parentSelector}'"
            ));
            return;
        }

        entity.SetBehavior<ParentBehavior, EntitySelector>(
            in selector,
            static (ref readonly EntitySelector _selector, ref ParentBehavior b) => b = b with { ParentSelector = _selector }
        );
    }

    private readonly record struct TransformData(Vector3 Position, Quaternion Rotation, Vector3 Scale, Vector3 Anchor);

    private static void WriteTransform(Entity entity, JsonObject transformObj)
    {
        // Get current transform or use defaults if not present
        entity.TryGetBehavior<TransformBehavior>(out var currentTransformNullable);
        var currentTransform = currentTransformNullable ?? default(TransformBehavior);
        
        var position = currentTransform.Position;
        var rotation = currentTransform.Rotation;
        var scale = currentTransform.Scale;
        var anchor = currentTransform.Anchor;
        var hasError = false;

        // Update position if present
        if (transformObj.TryGetPropertyValue("position", out var positionNode) && positionNode is JsonObject positionObj)
        {
            if (!positionObj.TryGetVector3Value(position, out position))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Transform position mutation failed: expected numeric x/y/z values"
                ));
                hasError = true;
            }
        }

        // Update rotation if present
        if (transformObj.TryGetPropertyValue("rotation", out var rotationNode) && rotationNode is JsonObject rotationObj)
        {
            if (!rotationObj.TryGetQuaternionValue(rotation, out rotation))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Transform rotation mutation failed: expected numeric x/y/z/w values"
                ));
                hasError = true;
            }
        }

        // Update scale if present
        if (transformObj.TryGetPropertyValue("scale", out var scaleNode) && scaleNode is JsonObject scaleObj)
        {
            if (!scaleObj.TryGetVector3Value(scale, out scale))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Transform scale mutation failed: expected numeric x/y/z values"
                ));
                hasError = true;
            }
        }

        // Update anchor if present
        if (transformObj.TryGetPropertyValue("anchor", out var anchorNode) && anchorNode is JsonObject anchorObj)
        {
            if (!anchorObj.TryGetVector3Value(anchor, out anchor))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Transform anchor mutation failed: expected numeric x/y/z values"
                ));
                hasError = true;
            }
        }

        // Only apply if no errors
        if (!hasError)
        {
            var data = new TransformData(position, rotation, scale, anchor);
            entity.SetBehavior<TransformBehavior, TransformData>(
                in data,
                static (ref readonly _data, ref b) => b = b with 
                {
                    Position = _data.Position,
                    Rotation = _data.Rotation,
                    Scale = _data.Scale,
                    Anchor = _data.Anchor
                }
            );
        }
    }


    private static void WriteFlexBehaviors(Entity entity, JsonObject entityObj)
    {
        // FlexAlignItems
        if (entityObj.TryGetPropertyValue("flexAlignItems", out var alignItemsNode) && alignItemsNode is JsonValue alignItemsValue)
        {
            if (!alignItemsValue.TryGetValue<string>(out var alignItemsStr) || 
                !Enum.TryParse<Align>(alignItemsStr, true, out var alignItems))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"FlexAlignItems mutation failed: expected valid Align enum value"
                ));
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
        if (entityObj.TryGetPropertyValue("flexAlignSelf", out var alignSelfNode) && alignSelfNode is JsonValue alignSelfValue)
        {
            if (!alignSelfValue.TryGetValue<string>(out var alignSelfStr) || 
                !Enum.TryParse<Align>(alignSelfStr, true, out var alignSelf))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"FlexAlignSelf mutation failed: expected valid Align enum value"
                ));
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
        if (entityObj.TryGetPropertyValue("flexBorderBottom", out var borderBottomNode) && borderBottomNode is JsonValue borderBottomValue)
        {
            if (!borderBottomValue.TryGetFloatValue(out var borderBottom))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"FlexBorderBottom mutation failed: expected numeric value"
                ));
            }
            else
            {
                entity.SetBehavior<FlexBorderBottomBehavior, float>(
                    in borderBottom,
                    static (ref readonly float val, ref FlexBorderBottomBehavior b) => b = new FlexBorderBottomBehavior(val)
                );
            }
        }

        // FlexBorderLeft
        if (entityObj.TryGetPropertyValue("flexBorderLeft", out var borderLeftNode) && borderLeftNode is JsonValue borderLeftValue)
        {
            if (!borderLeftValue.TryGetFloatValue(out var borderLeft))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"FlexBorderLeft mutation failed: expected numeric value"
                ));
            }
            else
            {
                entity.SetBehavior<FlexBorderLeftBehavior, float>(
                    in borderLeft,
                    static (ref readonly float val, ref FlexBorderLeftBehavior b) => b = new FlexBorderLeftBehavior(val)
                );
            }
        }

        // FlexBorderRight
        if (entityObj.TryGetPropertyValue("flexBorderRight", out var borderRightNode) && borderRightNode is JsonValue borderRightValue)
        {
            if (borderRightValue.TryGetFloatValue(out var borderRight))
            {
                entity.SetBehavior<FlexBorderRightBehavior, float>(
                    in borderRight,
                    static (ref readonly float val, ref FlexBorderRightBehavior b) => b = new FlexBorderRightBehavior(val)
                );
            }
        }

        // FlexBorderTop
        if (entityObj.TryGetPropertyValue("flexBorderTop", out var borderTopNode) && borderTopNode is JsonValue borderTopValue)
        {
            if (borderTopValue.TryGetFloatValue(out var borderTop))
            {
                entity.SetBehavior<FlexBorderTopBehavior, float>(
                    in borderTop,
                    static (ref readonly float val, ref FlexBorderTopBehavior b) => b = new FlexBorderTopBehavior(val)
                );
            }
        }

        // FlexDirection
        if (entityObj.TryGetPropertyValue("flexDirection", out var directionNode) && directionNode is JsonValue directionValue)
        {
            if (!directionValue.TryGetValue<string>(out var directionStr) || 
                !Enum.TryParse<FlexDirection>(directionStr, true, out var direction))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"FlexDirection mutation failed: expected valid FlexDirection enum value"
                ));
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
        if (entityObj.TryGetPropertyValue("flexGrow", out var growNode) && growNode is JsonValue growValue)
        {
            if (!growValue.TryGetFloatValue(out var grow))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"FlexGrow mutation failed: expected numeric value"
                ));
            }
            else
            {
                entity.SetBehavior<FlexGrowBehavior, float>(
                    in grow,
                    static (ref readonly float val, ref FlexGrowBehavior b) => b = new FlexGrowBehavior(val)
                );
            }
        }

        // FlexWrap
        if (entityObj.TryGetPropertyValue("flexWrap", out var wrapNode) && wrapNode is JsonValue wrapValue)
        {
            if (!wrapValue.TryGetValue<string>(out var wrapStr) || 
                !Enum.TryParse<Wrap>(wrapStr, true, out var wrap))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"FlexWrap mutation failed: expected valid Wrap enum value"
                ));
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
        if (entityObj.TryGetPropertyValue("flexZOverride", out var zOverrideNode) && zOverrideNode is JsonValue zOverrideValue)
        {
            if (!zOverrideValue.TryGetValue<int>(out var zOverride))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"FlexZOverride mutation failed: expected integer value"
                ));
            }
            else
            {
                entity.SetBehavior<FlexZOverride, int>(
                    in zOverride,
                    static (ref readonly int val, ref FlexZOverride b) => b = new FlexZOverride(val)
                );
            }
        }

        // FlexHeight
        if (entityObj.TryGetPropertyValue("flexHeight", out var heightNode) && heightNode is JsonValue heightValue)
        {
            if (!heightValue.TryGetFloatValue(out var height))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"FlexHeight mutation failed: expected numeric value"
                ));
            }
            else
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexHeightPercent", out var heightPercentNode) && 
                    heightPercentNode is JsonValue heightPercentValue)
                {
                    heightPercentValue.TryGetValue<bool>(out percent);
                }
                var data = (height, percent);
                entity.SetBehavior<FlexHeightBehavior, (float, bool)>(
                    in data,
                    static (ref readonly (float Height, bool Percent) val, ref FlexHeightBehavior b) => b = new FlexHeightBehavior(val.Height, val.Percent)
                );
            }
        }

        // FlexJustifyContent
        if (entityObj.TryGetPropertyValue("flexJustifyContent", out var justifyNode) && justifyNode is JsonValue justifyValue)
        {
            if (!justifyValue.TryGetValue<string>(out var justifyStr) || 
                !Enum.TryParse<Justify>(justifyStr, true, out var justify))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"FlexJustifyContent mutation failed: expected valid Justify enum value"
                ));
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
        if (entityObj.TryGetPropertyValue("flexMarginBottom", out var marginBottomNode) && marginBottomNode is JsonValue marginBottomValue)
        {
            if (marginBottomValue.TryGetFloatValue(out var marginBottom))
            {
                entity.SetBehavior<FlexMarginBottomBehavior, float>(
                    in marginBottom,
                    static (ref readonly float val, ref FlexMarginBottomBehavior b) => b = new FlexMarginBottomBehavior(val)
                );
            }
        }

        // FlexMarginLeft
        if (entityObj.TryGetPropertyValue("flexMarginLeft", out var marginLeftNode) && marginLeftNode is JsonValue marginLeftValue)
        {
            if (!marginLeftValue.TryGetFloatValue(out var marginLeft))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"FlexMarginLeft mutation failed: expected numeric value"
                ));
            }
            else
            {
                entity.SetBehavior<FlexMarginLeftBehavior, float>(
                    in marginLeft,
                    static (ref readonly float val, ref FlexMarginLeftBehavior b) => b = new FlexMarginLeftBehavior(val)
                );
            }
        }

        // FlexMarginRight
        if (entityObj.TryGetPropertyValue("flexMarginRight", out var marginRightNode) && marginRightNode is JsonValue marginRightValue)
        {
            if (marginRightValue.TryGetFloatValue(out var marginRight))
            {
                entity.SetBehavior<FlexMarginRightBehavior, float>(
                    in marginRight,
                    static (ref readonly float val, ref FlexMarginRightBehavior b) => b = new FlexMarginRightBehavior(val)
                );
            }
        }

        // FlexMarginTop
        if (entityObj.TryGetPropertyValue("flexMarginTop", out var marginTopNode) && marginTopNode is JsonValue marginTopValue)
        {
            if (marginTopValue.TryGetFloatValue(out var marginTop))
            {
                entity.SetBehavior<FlexMarginTopBehavior, float>(
                    in marginTop,
                    static (ref readonly float val, ref FlexMarginTopBehavior b) => b = new FlexMarginTopBehavior(val)
                );
            }
        }

        // FlexPaddingBottom
        if (entityObj.TryGetPropertyValue("flexPaddingBottom", out var paddingBottomNode) && paddingBottomNode is JsonValue paddingBottomValue)
        {
            if (paddingBottomValue.TryGetFloatValue(out var paddingBottom))
            {
                entity.SetBehavior<FlexPaddingBottomBehavior, float>(
                    in paddingBottom,
                    static (ref readonly float val, ref FlexPaddingBottomBehavior b) => b = new FlexPaddingBottomBehavior(val)
                );
            }
        }

        // FlexPaddingLeft
        if (entityObj.TryGetPropertyValue("flexPaddingLeft", out var paddingLeftNode) && paddingLeftNode is JsonValue paddingLeftValue)
        {
            if (!paddingLeftValue.TryGetFloatValue(out var paddingLeft))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"FlexPaddingLeft mutation failed: expected numeric value"
                ));
            }
            else
            {
                entity.SetBehavior<FlexPaddingLeftBehavior, float>(
                    in paddingLeft,
                    static (ref readonly float val, ref FlexPaddingLeftBehavior b) => b = new FlexPaddingLeftBehavior(val)
                );
            }
        }

        // FlexPaddingRight
        if (entityObj.TryGetPropertyValue("flexPaddingRight", out var paddingRightNode) && paddingRightNode is JsonValue paddingRightValue)
        {
            if (paddingRightValue.TryGetFloatValue(out var paddingRight))
            {
                entity.SetBehavior<FlexPaddingRightBehavior, float>(
                    in paddingRight,
                    static (ref readonly float val, ref FlexPaddingRightBehavior b) => b = new FlexPaddingRightBehavior(val)
                );
            }
        }

        // FlexPaddingTop
        if (entityObj.TryGetPropertyValue("flexPaddingTop", out var paddingTopNode) && paddingTopNode is JsonValue paddingTopValue)
        {
            if (paddingTopValue.TryGetFloatValue(out var paddingTop))
            {
                entity.SetBehavior<FlexPaddingTopBehavior, float>(
                    in paddingTop,
                    static (ref readonly float val, ref FlexPaddingTopBehavior b) => b = new FlexPaddingTopBehavior(val)
                );
            }
        }

        // FlexPositionBottom
        if (entityObj.TryGetPropertyValue("flexPositionBottom", out var positionBottomNode) && positionBottomNode is JsonValue positionBottomValue)
        {
            if (positionBottomValue.TryGetFloatValue(out var positionBottom))
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexPositionBottomPercent", out var positionBottomPercentNode) && 
                    positionBottomPercentNode is JsonValue positionBottomPercentValue)
                {
                    positionBottomPercentValue.TryGetValue<bool>(out percent);
                }
                var data = (positionBottom, percent);
                entity.SetBehavior<FlexPositionBottomBehavior, (float, bool)>(
                    in data,
                    static (ref readonly (float Value, bool Percent) val, ref FlexPositionBottomBehavior b) => b = new FlexPositionBottomBehavior(val.Value, val.Percent)
                );
            }
        }

        // FlexPositionLeft
        if (entityObj.TryGetPropertyValue("flexPositionLeft", out var positionLeftNode) && positionLeftNode is JsonValue positionLeftValue)
        {
            if (!positionLeftValue.TryGetFloatValue(out var positionLeft))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"FlexPositionLeft mutation failed: expected numeric value"
                ));
            }
            else
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexPositionLeftPercent", out var positionLeftPercentNode) && 
                    positionLeftPercentNode is JsonValue positionLeftPercentValue)
                {
                    positionLeftPercentValue.TryGetValue<bool>(out percent);
                }
                var data = (positionLeft, percent);
                entity.SetBehavior<FlexPositionLeftBehavior, (float, bool)>(
                    in data,
                    static (ref readonly (float Value, bool Percent) val, ref FlexPositionLeftBehavior b) => b = new FlexPositionLeftBehavior(val.Value, val.Percent)
                );
            }
        }

        // FlexPositionRight
        if (entityObj.TryGetPropertyValue("flexPositionRight", out var positionRightNode) && positionRightNode is JsonValue positionRightValue)
        {
            if (positionRightValue.TryGetFloatValue(out var positionRight))
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexPositionRightPercent", out var positionRightPercentNode) && 
                    positionRightPercentNode is JsonValue positionRightPercentValue)
                {
                    positionRightPercentValue.TryGetValue<bool>(out percent);
                }
                var data = (positionRight, percent);
                entity.SetBehavior<FlexPositionRightBehavior, (float, bool)>(
                    in data,
                    static (ref readonly (float Value, bool Percent) val, ref FlexPositionRightBehavior b) => b = new FlexPositionRightBehavior(val.Value, val.Percent)
                );
            }
        }

        // FlexPositionTop
        if (entityObj.TryGetPropertyValue("flexPositionTop", out var positionTopNode) && positionTopNode is JsonValue positionTopValue)
        {
            if (positionTopValue.TryGetFloatValue(out var positionTop))
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexPositionTopPercent", out var positionTopPercentNode) && 
                    positionTopPercentNode is JsonValue positionTopPercentValue)
                {
                    positionTopPercentValue.TryGetValue<bool>(out percent);
                }
                var data = (positionTop, percent);
                entity.SetBehavior<FlexPositionTopBehavior, (float, bool)>(
                    in data,
                    static (ref readonly (float Value, bool Percent) val, ref FlexPositionTopBehavior b) => b = new FlexPositionTopBehavior(val.Value, val.Percent)
                );
            }
        }

        // FlexPositionType
        if (entityObj.TryGetPropertyValue("flexPositionType", out var positionTypeNode) && positionTypeNode is JsonValue positionTypeValue)
        {
            if (!positionTypeValue.TryGetValue<string>(out var positionTypeStr) || 
                !Enum.TryParse<PositionType>(positionTypeStr, true, out var positionType))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"FlexPositionType mutation failed: expected valid PositionType enum value"
                ));
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
        if (entityObj.TryGetPropertyValue("flexWidth", out var widthNode) && widthNode is JsonValue widthValue)
        {
            if (!widthValue.TryGetFloatValue(out var width))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"FlexWidth mutation failed: expected numeric value"
                ));
            }
            else
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexWidthPercent", out var widthPercentNode) && 
                    widthPercentNode is JsonValue widthPercentValue)
                {
                    widthPercentValue.TryGetValue<bool>(out percent);
                }
                var data = (width, percent);
                entity.SetBehavior<FlexWidthBehavior, (float, bool)>(
                    in data,
                    static (ref readonly (float Value, bool Percent) val, ref FlexWidthBehavior b) => b = new FlexWidthBehavior(val.Value, val.Percent)
                );
            }
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

    private static Vector3 ParseVector3(JsonObject obj, Vector3 defaultValue)
    {
        var x = defaultValue.X;
        var y = defaultValue.Y;
        var z = defaultValue.Z;

        if (obj.TryGetPropertyValue("x", out var xNode) && xNode is JsonValue xValue)
        {
            x = xValue.TryGetValue<float>(out var xFloat) ? xFloat : 
                xValue.TryGetValue<double>(out var xDouble) ? (float)xDouble :
                xValue.TryGetValue<int>(out var xInt) ? xInt : x;
        }

        if (obj.TryGetPropertyValue("y", out var yNode) && yNode is JsonValue yValue)
        {
            y = yValue.TryGetValue<float>(out var yFloat) ? yFloat : 
                yValue.TryGetValue<double>(out var yDouble) ? (float)yDouble :
                yValue.TryGetValue<int>(out var yInt) ? yInt : y;
        }

        if (obj.TryGetPropertyValue("z", out var zNode) && zNode is JsonValue zValue)
        {
            z = zValue.TryGetValue<float>(out var zFloat) ? zFloat : 
                zValue.TryGetValue<double>(out var zDouble) ? (float)zDouble :
                zValue.TryGetValue<int>(out var zInt) ? zInt : z;
        }

        return new Vector3(x, y, z);
    }

    private static Quaternion ParseQuaternion(JsonObject obj, Quaternion defaultValue)
    {
        var x = defaultValue.X;
        var y = defaultValue.Y;
        var z = defaultValue.Z;
        var w = defaultValue.W;

        if (obj.TryGetPropertyValue("x", out var xNode) && xNode is JsonValue xValue)
        {
            x = xValue.TryGetValue<float>(out var xFloat) ? xFloat : 
                xValue.TryGetValue<double>(out var xDouble) ? (float)xDouble : x;
        }

        if (obj.TryGetPropertyValue("y", out var yNode) && yNode is JsonValue yValue)
        {
            y = yValue.TryGetValue<float>(out var yFloat) ? yFloat : 
                yValue.TryGetValue<double>(out var yDouble) ? (float)yDouble : y;
        }

        if (obj.TryGetPropertyValue("z", out var zNode) && zNode is JsonValue zValue)
        {
            z = zValue.TryGetValue<float>(out var zFloat) ? zFloat : 
                zValue.TryGetValue<double>(out var zDouble) ? (float)zDouble : z;
        }

        if (obj.TryGetPropertyValue("w", out var wNode) && wNode is JsonValue wValue)
        {
            w = wValue.TryGetValue<float>(out var wFloat) ? wFloat : 
                wValue.TryGetValue<double>(out var wDouble) ? (float)wDouble : w;
        }

        return new Quaternion(x, y, z, w);
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
}
