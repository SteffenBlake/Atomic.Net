// This file is part of JsonEntityConverter - Write methods for applying JsonNode changes back to Entity behaviors
// Extracted from RulesDriver.cs refactor - applies all mutation logic from JsonNode to Entity

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
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Scenes;

public static partial class JsonEntityConverter
{
    /// <summary>
    /// Writes a mutated JsonNode back to the Entity behaviors.
    /// This applies all changes from the JsonNode to the actual Entity.
    /// </summary>
    public static void Write(JsonNode entityJson, Entity entity)
    {
        if (entityJson is not JsonObject entityObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Cannot write entity - JSON is not a JsonObject"
            ));
            return;
        }

        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Cannot write to inactive entity"
            ));
            return;
        }

        // Apply each behavior field from the JsonNode
        WriteProperties(entityObj, entity);
        WriteId(entityObj, entity);
        WriteTags(entityObj, entity);
        WriteTransform(entityObj, entity);
        WriteParent(entityObj, entity);
        WriteFlexBehaviors(entityObj, entity);
    }

    private static void WriteProperties(JsonObject entityObj, Entity entity)
    {
        if (!entityObj.TryGetPropertyValue("properties", out var propertiesNode) || propertiesNode is not JsonObject propertiesObj)
        {
            return;
        }

        // Clear existing properties first
        entity.SetBehavior<PropertiesBehavior>(
            static (ref b) =>
            {
                b.Properties.Clear();
                b = b with { Properties = b.Properties };
            }
        );

        foreach (var (key, valueNode) in propertiesObj)
        {
            if (valueNode == null)
            {
                continue;
            }

            if (!TryConvertToPropertyValue(valueNode, out var propertyValue))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Failed to convert property '{key}' value"
                ));
                continue;
            }

            var setter = (key, propertyValue.Value);
            entity.SetBehavior<PropertiesBehavior, (string Key, PropertyValue Value)>(
                ref setter,
                static (ref readonly _setter, ref b) => b = b with { 
                    Properties = b.Properties.With(_setter.Key, _setter.Value) 
                }
            );
        }
    }

    private static void WriteId(JsonObject entityObj, Entity entity)
    {
        if (!entityObj.TryGetPropertyValue("id", out var idNode) || idNode == null)
        {
            return;
        }

        if (!TryGetStringValue(idNode, out var newId))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Failed to parse id value. Id must be a string."
            ));
            return;
        }

        entity.SetBehavior<IdBehavior, string>(
            in newId,
            static (ref readonly _newId, ref b) => b = b with { Id = _newId }
        );
    }

    private static void WriteTags(JsonObject entityObj, Entity entity)
    {
        if (!entityObj.TryGetPropertyValue("tags", out var tagsNode) || tagsNode is not JsonArray tagsArray)
        {
            return;
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
                    $"Tag at index {i} is null"
                ));
                continue;
            }

            if (!TryGetStringValue(tagNode, out var tag))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Failed to parse tag at index {i}. Tags must be strings."
                ));
                continue;
            }

            entity.SetBehavior<TagsBehavior, string>(
                in tag,
                static (ref readonly _tag, ref b) => b = b with { Tags = b.Tags.With(_tag) }
            );
        }
    }

    private static void WriteTransform(JsonObject entityObj, Entity entity)
    {
        if (!entityObj.TryGetPropertyValue("transform", out var transformNode) || transformNode is not JsonObject transformObj)
        {
            return;
        }

        if (transformObj.TryGetPropertyValue("position", out var posNode) && posNode != null)
        {
            if (TryParseVector3(posNode, "position", out var vector))
            {
                var val = vector.Value;
                entity.SetBehavior<TransformBehavior, Vector3>(
                    in val,
                    static (ref readonly _vec, ref b) => b.Position = _vec
                );
            }
        }

        if (transformObj.TryGetPropertyValue("rotation", out var rotNode) && rotNode != null)
        {
            if (TryParseQuaternion(rotNode, "rotation", out var quat))
            {
                var val = quat.Value;
                entity.SetBehavior<TransformBehavior, Quaternion>(
                    in val,
                    static (ref readonly _quat, ref b) => b.Rotation = _quat
                );
            }
        }

        if (transformObj.TryGetPropertyValue("scale", out var scaleNode) && scaleNode != null)
        {
            if (TryParseVector3(scaleNode, "scale", out var vector))
            {
                var val = vector.Value;
                entity.SetBehavior<TransformBehavior, Vector3>(
                    in val,
                    static (ref readonly _vec, ref b) => b.Scale = _vec
                );
            }
        }

        if (transformObj.TryGetPropertyValue("anchor", out var anchorNode) && anchorNode != null)
        {
            if (TryParseVector3(anchorNode, "anchor", out var vector))
            {
                var val = vector.Value;
                entity.SetBehavior<TransformBehavior, Vector3>(
                    in val,
                    static (ref readonly _vec, ref b) => b.Anchor = _vec
                );
            }
        }
    }

    private static void WriteParent(JsonObject entityObj, Entity entity)
    {
        if (!entityObj.TryGetPropertyValue("parent", out var parentNode) || parentNode == null)
        {
            return;
        }

        if (!TryGetParentSelector(parentNode, out var selector))
        {
            return;
        }

        var parentBehavior = new ParentBehavior(selector);
        entity.SetBehavior<ParentBehavior, ParentBehavior>(
            in parentBehavior,
            static (ref readonly _parent, ref b) => b = _parent
        );
    }

    private static bool TryGetParentSelector(
        JsonNode parentNode,
        [NotNullWhen(true)]
        out EntitySelector? selector
    )
    {
        if (!TryGetStringValue(parentNode, out var selectorString))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Failed to parse parent selector. Parent must be a string."
            ));
            selector = null;
            return false;
        }

        if (!SelectorRegistry.Instance.TryParse(selectorString.AsSpan(), out selector))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse parent selector '{selectorString}'"
            ));
            return false;
        }

        return true;
    }

    private static void WriteFlexBehaviors(JsonObject entityObj, Entity entity)
    {
        if (entityObj.TryGetPropertyValue("flexAlignItems", out var alignItemsNode) && alignItemsNode != null)
        {
            if (TryParseEnum<Align>(alignItemsNode, "flexAlignItems", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexAlignItemsBehavior, Align>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexAlignSelf", out var alignSelfNode) && alignSelfNode != null)
        {
            if (TryParseEnum<Align>(alignSelfNode, "flexAlignSelf", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexAlignSelfBehavior, Align>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexBorderBottom", out var borderBottomNode) && borderBottomNode != null)
        {
            if (TryParseFloat(borderBottomNode, "flexBorderBottom", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexBorderBottomBehavior, float>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexBorderLeft", out var borderLeftNode) && borderLeftNode != null)
        {
            if (TryParseFloat(borderLeftNode, "flexBorderLeft", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexBorderLeftBehavior, float>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexBorderRight", out var borderRightNode) && borderRightNode != null)
        {
            if (TryParseFloat(borderRightNode, "flexBorderRight", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexBorderRightBehavior, float>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexBorderTop", out var borderTopNode) && borderTopNode != null)
        {
            if (TryParseFloat(borderTopNode, "flexBorderTop", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexBorderTopBehavior, float>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexDirection", out var directionNode) && directionNode != null)
        {
            if (TryParseEnum<FlexDirection>(directionNode, "flexDirection", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexDirectionBehavior, FlexDirection>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexGrow", out var growNode) && growNode != null)
        {
            if (TryParseFloat(growNode, "flexGrow", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexGrowBehavior, float>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexWrap", out var wrapNode) && wrapNode != null)
        {
            if (TryParseEnum<Wrap>(wrapNode, "flexWrap", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexWrapBehavior, Wrap>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexZOverride", out var zOverrideNode) && zOverrideNode != null)
        {
            if (TryParseInt(zOverrideNode, "flexZOverride", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexZOverride, int>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { ZIndex = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexHeight", out var heightNode) && heightNode != null)
        {
            if (TryParseFloat(heightNode, "flexHeight", out var value))
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexHeightPercent", out var percentNode) && percentNode != null)
                {
                    percent = percentNode.GetValue<bool>();
                }
                
                var behavior = new FlexHeightBehavior(value.Value, percent);
                entity.SetBehavior<FlexHeightBehavior, FlexHeightBehavior>(
                    in behavior,
                    static (ref readonly _b, ref b) => b = _b
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexJustifyContent", out var justifyNode) && justifyNode != null)
        {
            if (TryParseEnum<Justify>(justifyNode, "flexJustifyContent", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexJustifyContentBehavior, Justify>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexMarginBottom", out var marginBottomNode) && marginBottomNode != null)
        {
            if (TryParseFloat(marginBottomNode, "flexMarginBottom", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexMarginBottomBehavior, float>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexMarginLeft", out var marginLeftNode) && marginLeftNode != null)
        {
            if (TryParseFloat(marginLeftNode, "flexMarginLeft", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexMarginLeftBehavior, float>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexMarginRight", out var marginRightNode) && marginRightNode != null)
        {
            if (TryParseFloat(marginRightNode, "flexMarginRight", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexMarginRightBehavior, float>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexMarginTop", out var marginTopNode) && marginTopNode != null)
        {
            if (TryParseFloat(marginTopNode, "flexMarginTop", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexMarginTopBehavior, float>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPaddingBottom", out var paddingBottomNode) && paddingBottomNode != null)
        {
            if (TryParseFloat(paddingBottomNode, "flexPaddingBottom", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexPaddingBottomBehavior, float>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPaddingLeft", out var paddingLeftNode) && paddingLeftNode != null)
        {
            if (TryParseFloat(paddingLeftNode, "flexPaddingLeft", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexPaddingLeftBehavior, float>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPaddingRight", out var paddingRightNode) && paddingRightNode != null)
        {
            if (TryParseFloat(paddingRightNode, "flexPaddingRight", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexPaddingRightBehavior, float>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPaddingTop", out var paddingTopNode) && paddingTopNode != null)
        {
            if (TryParseFloat(paddingTopNode, "flexPaddingTop", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexPaddingTopBehavior, float>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPositionBottom", out var posBottomNode) && posBottomNode != null)
        {
            if (TryParseFloat(posBottomNode, "flexPositionBottom", out var value))
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexPositionBottomPercent", out var percentNode) && percentNode != null)
                {
                    percent = percentNode.GetValue<bool>();
                }
                
                var behavior = new FlexPositionBottomBehavior(value.Value, percent);
                entity.SetBehavior<FlexPositionBottomBehavior, FlexPositionBottomBehavior>(
                    in behavior,
                    static (ref readonly _b, ref b) => b = _b
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPositionLeft", out var posLeftNode) && posLeftNode != null)
        {
            if (TryParseFloat(posLeftNode, "flexPositionLeft", out var value))
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexPositionLeftPercent", out var percentNode) && percentNode != null)
                {
                    percent = percentNode.GetValue<bool>();
                }
                
                var behavior = new FlexPositionLeftBehavior(value.Value, percent);
                entity.SetBehavior<FlexPositionLeftBehavior, FlexPositionLeftBehavior>(
                    in behavior,
                    static (ref readonly _b, ref b) => b = _b
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPositionRight", out var posRightNode) && posRightNode != null)
        {
            if (TryParseFloat(posRightNode, "flexPositionRight", out var value))
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexPositionRightPercent", out var percentNode) && percentNode != null)
                {
                    percent = percentNode.GetValue<bool>();
                }
                
                var behavior = new FlexPositionRightBehavior(value.Value, percent);
                entity.SetBehavior<FlexPositionRightBehavior, FlexPositionRightBehavior>(
                    in behavior,
                    static (ref readonly _b, ref b) => b = _b
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPositionTop", out var posTopNode) && posTopNode != null)
        {
            if (TryParseFloat(posTopNode, "flexPositionTop", out var value))
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexPositionTopPercent", out var percentNode) && percentNode != null)
                {
                    percent = percentNode.GetValue<bool>();
                }
                
                var behavior = new FlexPositionTopBehavior(value.Value, percent);
                entity.SetBehavior<FlexPositionTopBehavior, FlexPositionTopBehavior>(
                    in behavior,
                    static (ref readonly _b, ref b) => b = _b
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPositionType", out var posTypeNode) && posTypeNode != null)
        {
            if (TryParseEnum<PositionType>(posTypeNode, "flexPositionType", out var value))
            {
                var val = value.Value;
                entity.SetBehavior<FlexPositionTypeBehavior, PositionType>(
                    in val,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexWidth", out var widthNode) && widthNode != null)
        {
            if (TryParseFloat(widthNode, "flexWidth", out var value))
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexWidthPercent", out var percentNode) && percentNode != null)
                {
                    percent = percentNode.GetValue<bool>();
                }
                
                var behavior = new FlexWidthBehavior(value.Value, percent);
                entity.SetBehavior<FlexWidthBehavior, FlexWidthBehavior>(
                    in behavior,
                    static (ref readonly _b, ref b) => b = _b
                );
            }
        }
    }

    // Helper methods for parsing values from JsonNode

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

    private static bool TryParseVector3(
        JsonNode value,
        string fieldName,
        [NotNullWhen(true)]
        out Vector3? result
    )
    {
        if (value is not JsonObject vecObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"{fieldName} must be a JsonObject with x, y, z fields"
            ));
            result = null;
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
                $"Failed to parse {fieldName}: {ex.Message}"
            ));
            result = null;
            return false;
        }
    }

    private static bool TryParseQuaternion(
        JsonNode value,
        string fieldName,
        [NotNullWhen(true)]
        out Quaternion? result
    )
    {
        if (value is not JsonObject quatObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"{fieldName} must be a JsonObject with x, y, z, w fields"
            ));
            result = null;
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
                $"Failed to parse {fieldName}: {ex.Message}"
            ));
            result = null;
            return false;
        }
    }

    private static bool TryParseEnum<TEnum>(
        JsonNode value,
        string fieldName,
        [NotNullWhen(true)]
        out TEnum? result
    )
        where TEnum : struct, Enum
    {
        if (!TryGetStringValue(value, out var stringValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName}. Expected string value."
            ));
            result = null;
            return false;
        }

        if (!Enum.TryParse<TEnum>(stringValue, ignoreCase: true, out var enumValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} value '{stringValue}'"
            ));
            result = null;
            return false;
        }

        result = enumValue;
        return true;
    }

    private static bool TryParseFloat(
        JsonNode value,
        string fieldName,
        [NotNullWhen(true)]
        out float? result
    )
    {
        try
        {
            if (value is JsonValue jsonValue && jsonValue.TryGetValue<float>(out var floatValue))
            {
                result = floatValue;
                return true;
            }

            // Try parsing as int first
            if (value is JsonValue jsonValueInt && jsonValueInt.TryGetValue<int>(out var intValue))
            {
                result = intValue;
                return true;
            }

            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName}. Expected numeric value."
            ));
            result = null;
            return false;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName}: {ex.Message}"
            ));
            result = null;
            return false;
        }
    }

    private static bool TryParseInt(
        JsonNode value,
        string fieldName,
        [NotNullWhen(true)]
        out int? result
    )
    {
        try
        {
            if (value is JsonValue jsonValue && jsonValue.TryGetValue<int>(out var intValue))
            {
                result = intValue;
                return true;
            }

            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName}. Expected integer value."
            ));
            result = null;
            return false;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName}: {ex.Message}"
            ));
            result = null;
            return false;
        }
    }

    private static bool TryGetStringValue(
        JsonNode node,
        [NotNullWhen(true)]
        out string? result
    )
    {
        try
        {
            result = node.GetValue<string>();
            return !string.IsNullOrWhiteSpace(result);
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to get string value: {ex.Message}"
            ));
            result = null;
            return false;
        }
    }
}
