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
    public static void Write(JsonNode entityJson, ushort entityIndex)
    {
        if (entityJson is not JsonObject entityObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Cannot write entity - JSON is not a JsonObject for entity {entityIndex}"
            ));
            return;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        if (!entity.Active)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Cannot write to inactive entity {entityIndex}"
            ));
            return;
        }

        // Apply each behavior field from the JsonNode
        WriteProperties(entityObj, entityIndex);
        WriteId(entityObj, entityIndex);
        WriteTags(entityObj, entityIndex);
        WriteTransform(entityObj, entityIndex);
        WriteParent(entityObj, entityIndex);
        WriteFlexBehaviors(entityObj, entityIndex);
    }

    private static void WriteProperties(JsonObject entityObj, ushort entityIndex)
    {
        if (!entityObj.TryGetPropertyValue("properties", out var propertiesNode) || propertiesNode is not JsonObject propertiesObj)
        {
            return;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        foreach (var (key, valueNode) in propertiesObj)
        {
            if (valueNode == null)
            {
                continue;
            }

            if (!TryConvertToPropertyValue(valueNode, out var propertyValue))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Failed to convert property '{key}' value for entity {entityIndex}"
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

    private static void WriteId(JsonObject entityObj, ushort entityIndex)
    {
        if (!entityObj.TryGetPropertyValue("id", out var idNode) || idNode == null)
        {
            return;
        }

        if (!TryGetStringValue(idNode, out var newId))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse id value for entity {entityIndex}. Id must be a string."
            ));
            return;
        }

        if (string.IsNullOrWhiteSpace(newId))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Id value cannot be null, empty, or whitespace for entity {entityIndex}"
            ));
            return;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        entity.SetBehavior<IdBehavior, string>(
            in newId,
            static (ref readonly _newId, ref b) => b = b with { Id = _newId }
        );
    }

    private static void WriteTags(JsonObject entityObj, ushort entityIndex)
    {
        if (!entityObj.TryGetPropertyValue("tags", out var tagsNode) || tagsNode is not JsonArray tagsArray)
        {
            return;
        }

        var entity = EntityRegistry.Instance[entityIndex];

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
                continue;
            }

            if (!TryGetStringValue(tagNode, out var tag))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Failed to parse tag at index {i} for entity {entityIndex}. Tags must be strings."
                ));
                continue;
            }

            if (string.IsNullOrWhiteSpace(tag))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Tag at index {i} cannot be null, empty, or whitespace for entity {entityIndex}"
                ));
                continue;
            }

            entity.SetBehavior<TagsBehavior, string>(
                in tag,
                static (ref readonly _tag, ref b) => b = b with { Tags = b.Tags.With(_tag) }
            );
        }
    }

    private static void WriteTransform(JsonObject entityObj, ushort entityIndex)
    {
        if (!entityObj.TryGetPropertyValue("transform", out var transformNode) || transformNode is not JsonObject transformObj)
        {
            return;
        }

        var entity = EntityRegistry.Instance[entityIndex];

        if (transformObj.TryGetPropertyValue("position", out var posNode) && posNode != null)
        {
            if (TryParseVector3(posNode, entityIndex, "position", out var vector))
            {
                entity.SetBehavior<TransformBehavior, Vector3>(
                    in vector,
                    static (ref readonly _vec, ref b) => b.Position = _vec
                );
            }
        }

        if (transformObj.TryGetPropertyValue("rotation", out var rotNode) && rotNode != null)
        {
            if (TryParseQuaternion(rotNode, entityIndex, "rotation", out var quat))
            {
                entity.SetBehavior<TransformBehavior, Quaternion>(
                    in quat,
                    static (ref readonly _quat, ref b) => b.Rotation = _quat
                );
            }
        }

        if (transformObj.TryGetPropertyValue("scale", out var scaleNode) && scaleNode != null)
        {
            if (TryParseVector3(scaleNode, entityIndex, "scale", out var vector))
            {
                entity.SetBehavior<TransformBehavior, Vector3>(
                    in vector,
                    static (ref readonly _vec, ref b) => b.Scale = _vec
                );
            }
        }

        if (transformObj.TryGetPropertyValue("anchor", out var anchorNode) && anchorNode != null)
        {
            if (TryParseVector3(anchorNode, entityIndex, "anchor", out var vector))
            {
                entity.SetBehavior<TransformBehavior, Vector3>(
                    in vector,
                    static (ref readonly _vec, ref b) => b.Anchor = _vec
                );
            }
        }
    }

    private static void WriteParent(JsonObject entityObj, ushort entityIndex)
    {
        if (!entityObj.TryGetPropertyValue("parent", out var parentNode) || parentNode == null)
        {
            return;
        }

        string selectorString;
        try
        {
            selectorString = parentNode.GetValue<string>();
        }
        catch
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse parent selector for entity {entityIndex}. Parent must be a string."
            ));
            return;
        }

        if (string.IsNullOrWhiteSpace(selectorString))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Parent selector cannot be null, empty, or whitespace for entity {entityIndex}"
            ));
            return;
        }

        if (!SelectorRegistry.Instance.TryParse(selectorString.AsSpan(), out var selector))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse parent selector '{selectorString}' for entity {entityIndex}"
            ));
            return;
        }

        var entity = EntityRegistry.Instance[entityIndex];
        var parentBehavior = new ParentBehavior(selector);
        entity.SetBehavior<ParentBehavior, ParentBehavior>(
            in parentBehavior,
            static (ref readonly _parent, ref b) => b = _parent
        );
    }

    private static void WriteFlexBehaviors(JsonObject entityObj, ushort entityIndex)
    {
        var entity = EntityRegistry.Instance[entityIndex];

        if (entityObj.TryGetPropertyValue("flexAlignItems", out var alignItemsNode) && alignItemsNode != null)
        {
            if (TryParseEnum<Align>(alignItemsNode, entityIndex, "flexAlignItems", out var value))
            {
                entity.SetBehavior<FlexAlignItemsBehavior, Align>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexAlignSelf", out var alignSelfNode) && alignSelfNode != null)
        {
            if (TryParseEnum<Align>(alignSelfNode, entityIndex, "flexAlignSelf", out var value))
            {
                entity.SetBehavior<FlexAlignSelfBehavior, Align>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexBorderBottom", out var borderBottomNode) && borderBottomNode != null)
        {
            if (TryParseFloat(borderBottomNode, entityIndex, "flexBorderBottom", out var value))
            {
                entity.SetBehavior<FlexBorderBottomBehavior, float>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexBorderLeft", out var borderLeftNode) && borderLeftNode != null)
        {
            if (TryParseFloat(borderLeftNode, entityIndex, "flexBorderLeft", out var value))
            {
                entity.SetBehavior<FlexBorderLeftBehavior, float>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexBorderRight", out var borderRightNode) && borderRightNode != null)
        {
            if (TryParseFloat(borderRightNode, entityIndex, "flexBorderRight", out var value))
            {
                entity.SetBehavior<FlexBorderRightBehavior, float>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexBorderTop", out var borderTopNode) && borderTopNode != null)
        {
            if (TryParseFloat(borderTopNode, entityIndex, "flexBorderTop", out var value))
            {
                entity.SetBehavior<FlexBorderTopBehavior, float>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexDirection", out var directionNode) && directionNode != null)
        {
            if (TryParseEnum<FlexDirection>(directionNode, entityIndex, "flexDirection", out var value))
            {
                entity.SetBehavior<FlexDirectionBehavior, FlexDirection>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexGrow", out var growNode) && growNode != null)
        {
            if (TryParseFloat(growNode, entityIndex, "flexGrow", out var value))
            {
                entity.SetBehavior<FlexGrowBehavior, float>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexWrap", out var wrapNode) && wrapNode != null)
        {
            if (TryParseEnum<Wrap>(wrapNode, entityIndex, "flexWrap", out var value))
            {
                entity.SetBehavior<FlexWrapBehavior, Wrap>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexZOverride", out var zOverrideNode) && zOverrideNode != null)
        {
            if (TryParseInt(zOverrideNode, entityIndex, "flexZOverride", out var value))
            {
                entity.SetBehavior<FlexZOverride, int>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { ZIndex = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexHeight", out var heightNode) && heightNode != null)
        {
            if (TryParseFloat(heightNode, entityIndex, "flexHeight", out var value))
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexHeightPercent", out var percentNode) && percentNode != null)
                {
                    percent = percentNode.GetValue<bool>();
                }
                
                var behavior = new FlexHeightBehavior(value, percent);
                entity.SetBehavior<FlexHeightBehavior, FlexHeightBehavior>(
                    in behavior,
                    static (ref readonly _b, ref b) => b = _b
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexJustifyContent", out var justifyNode) && justifyNode != null)
        {
            if (TryParseEnum<Justify>(justifyNode, entityIndex, "flexJustifyContent", out var value))
            {
                entity.SetBehavior<FlexJustifyContentBehavior, Justify>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexMarginBottom", out var marginBottomNode) && marginBottomNode != null)
        {
            if (TryParseFloat(marginBottomNode, entityIndex, "flexMarginBottom", out var value))
            {
                entity.SetBehavior<FlexMarginBottomBehavior, float>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexMarginLeft", out var marginLeftNode) && marginLeftNode != null)
        {
            if (TryParseFloat(marginLeftNode, entityIndex, "flexMarginLeft", out var value))
            {
                entity.SetBehavior<FlexMarginLeftBehavior, float>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexMarginRight", out var marginRightNode) && marginRightNode != null)
        {
            if (TryParseFloat(marginRightNode, entityIndex, "flexMarginRight", out var value))
            {
                entity.SetBehavior<FlexMarginRightBehavior, float>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexMarginTop", out var marginTopNode) && marginTopNode != null)
        {
            if (TryParseFloat(marginTopNode, entityIndex, "flexMarginTop", out var value))
            {
                entity.SetBehavior<FlexMarginTopBehavior, float>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPaddingBottom", out var paddingBottomNode) && paddingBottomNode != null)
        {
            if (TryParseFloat(paddingBottomNode, entityIndex, "flexPaddingBottom", out var value))
            {
                entity.SetBehavior<FlexPaddingBottomBehavior, float>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPaddingLeft", out var paddingLeftNode) && paddingLeftNode != null)
        {
            if (TryParseFloat(paddingLeftNode, entityIndex, "flexPaddingLeft", out var value))
            {
                entity.SetBehavior<FlexPaddingLeftBehavior, float>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPaddingRight", out var paddingRightNode) && paddingRightNode != null)
        {
            if (TryParseFloat(paddingRightNode, entityIndex, "flexPaddingRight", out var value))
            {
                entity.SetBehavior<FlexPaddingRightBehavior, float>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPaddingTop", out var paddingTopNode) && paddingTopNode != null)
        {
            if (TryParseFloat(paddingTopNode, entityIndex, "flexPaddingTop", out var value))
            {
                entity.SetBehavior<FlexPaddingTopBehavior, float>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPositionBottom", out var posBottomNode) && posBottomNode != null)
        {
            if (TryParseFloat(posBottomNode, entityIndex, "flexPositionBottom", out var value))
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexPositionBottomPercent", out var percentNode) && percentNode != null)
                {
                    percent = percentNode.GetValue<bool>();
                }
                
                var behavior = new FlexPositionBottomBehavior(value, percent);
                entity.SetBehavior<FlexPositionBottomBehavior, FlexPositionBottomBehavior>(
                    in behavior,
                    static (ref readonly _b, ref b) => b = _b
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPositionLeft", out var posLeftNode) && posLeftNode != null)
        {
            if (TryParseFloat(posLeftNode, entityIndex, "flexPositionLeft", out var value))
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexPositionLeftPercent", out var percentNode) && percentNode != null)
                {
                    percent = percentNode.GetValue<bool>();
                }
                
                var behavior = new FlexPositionLeftBehavior(value, percent);
                entity.SetBehavior<FlexPositionLeftBehavior, FlexPositionLeftBehavior>(
                    in behavior,
                    static (ref readonly _b, ref b) => b = _b
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPositionRight", out var posRightNode) && posRightNode != null)
        {
            if (TryParseFloat(posRightNode, entityIndex, "flexPositionRight", out var value))
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexPositionRightPercent", out var percentNode) && percentNode != null)
                {
                    percent = percentNode.GetValue<bool>();
                }
                
                var behavior = new FlexPositionRightBehavior(value, percent);
                entity.SetBehavior<FlexPositionRightBehavior, FlexPositionRightBehavior>(
                    in behavior,
                    static (ref readonly _b, ref b) => b = _b
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPositionTop", out var posTopNode) && posTopNode != null)
        {
            if (TryParseFloat(posTopNode, entityIndex, "flexPositionTop", out var value))
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexPositionTopPercent", out var percentNode) && percentNode != null)
                {
                    percent = percentNode.GetValue<bool>();
                }
                
                var behavior = new FlexPositionTopBehavior(value, percent);
                entity.SetBehavior<FlexPositionTopBehavior, FlexPositionTopBehavior>(
                    in behavior,
                    static (ref readonly _b, ref b) => b = _b
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexPositionType", out var posTypeNode) && posTypeNode != null)
        {
            if (TryParseEnum<PositionType>(posTypeNode, entityIndex, "flexPositionType", out var value))
            {
                entity.SetBehavior<FlexPositionTypeBehavior, PositionType>(
                    in value,
                    static (ref readonly _val, ref b) => b = b with { Value = _val }
                );
            }
        }

        if (entityObj.TryGetPropertyValue("flexWidth", out var widthNode) && widthNode != null)
        {
            if (TryParseFloat(widthNode, entityIndex, "flexWidth", out var value))
            {
                var percent = false;
                if (entityObj.TryGetPropertyValue("flexWidthPercent", out var percentNode) && percentNode != null)
                {
                    percent = percentNode.GetValue<bool>();
                }
                
                var behavior = new FlexWidthBehavior(value, percent);
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

    private static bool TryParseEnum<TEnum>(JsonNode value, ushort entityIndex, string fieldName, out TEnum result)
        where TEnum : struct, Enum
    {
        if (!TryGetStringValue(value, out var stringValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} for entity {entityIndex}. Expected string value."
            ));
            result = default;
            return false;
        }

        if (!Enum.TryParse<TEnum>(stringValue, ignoreCase: true, out result))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} value '{stringValue}' for entity {entityIndex}"
            ));
            return false;
        }

        return true;
    }

    private static bool TryParseFloat(JsonNode value, ushort entityIndex, string fieldName, out float result)
    {
        try
        {
            if (value is JsonValue jsonValue && jsonValue.TryGetValue<float>(out result))
            {
                return true;
            }

            // Try parsing as int first
            if (value is JsonValue jsonValueInt && jsonValueInt.TryGetValue<int>(out var intValue))
            {
                result = intValue;
                return true;
            }

            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} for entity {entityIndex}. Expected numeric value."
            ));
            result = default;
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

    private static bool TryParseInt(JsonNode value, ushort entityIndex, string fieldName, out int result)
    {
        try
        {
            if (value is JsonValue jsonValue && jsonValue.TryGetValue<int>(out result))
            {
                return true;
            }

            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} for entity {entityIndex}. Expected integer value."
            ));
            result = default;
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

    private static bool TryGetStringValue(
        JsonNode node,
        [NotNullWhen(true)]
        out string? result
    )
    {
        try
        {
            result = node.GetValue<string>();
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }
}
