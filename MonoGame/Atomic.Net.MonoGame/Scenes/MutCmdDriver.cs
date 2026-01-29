using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;
using Json.Logic;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Executes MutCommand operations by mutating entity JsonNodes in-place.
/// Does NOT write changes back to actual Entity behaviors - that's handled by JsonEntityConverter.Write.
/// </summary>
public static class MutCmdDriver
{
    // Pre-allocated array to avoid allocation on every error
    private static readonly string[] ValidTargets = [
        "properties", "id", "tags", "transform", "parent",
        "flexAlignItems", "flexAlignSelf",
        "flexBorderBottom", "flexBorderLeft", "flexBorderRight", "flexBorderTop",
        "flexDirection", "flexGrow", "flexWrap", "flexZOverride",
        "flexHeight", "flexJustifyContent",
        "flexMarginBottom", "flexMarginLeft", "flexMarginRight", "flexMarginTop",
        "flexPaddingBottom", "flexPaddingLeft", "flexPaddingRight", "flexPaddingTop",
        "flexPositionBottom", "flexPositionLeft", "flexPositionRight", "flexPositionTop",
        "flexPositionType",
        "flexWidth"
    ];

    public static void Execute(MutCommand command, JsonNode context, JsonNode entityJson)
    {
        foreach (var operation in command.Operations)
        {
            ApplyOperation(operation, context, entityJson);
        }
    }

    private static void ApplyOperation(MutOperation operation, JsonNode context, JsonNode entityJson)
    {
        if (entityJson is not JsonObject entityObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Entity JSON is not a JsonObject"));
            return;
        }

        if (!TryApplyJsonLogic(operation.Value, context, out var computedValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Failed to evaluate mutation value"));
            return;
        }

        // Clone the computed value before assignment
        // Per DISCOVERIES.md: JsonLogic.Apply() returns the original node reference for literals,
        // which still has a parent (the deserialized logic tree). We can't remove from parent
        // because the logic tree needs to be reused. DeepClone() is necessary here.
        var clonedValue = computedValue.DeepClone();

        if (operation.Target is not JsonObject targetObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Target is not a JsonObject"));
            return;
        }

        ApplyToEntityJson(targetObj, clonedValue, entityObj);
    }

    private static void ApplyToEntityJson(JsonObject target, JsonNode value, JsonObject entityJson)
    {
        if (target.TryGetPropertyValue("properties", out var propertyKeyNode))
        {
            if (!TryGetStringValue(propertyKeyNode, out var propertyKey))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("Failed to parse property key"));
                return;
            }

            if (!entityJson.TryGetPropertyValue("properties", out var propsNode) || propsNode is not JsonObject propertiesObj)
            {
                propertiesObj = new JsonObject();
                entityJson["properties"] = propertiesObj;
            }
            else
            {
                propertiesObj = (JsonObject)propsNode;
            }

            propertiesObj[propertyKey] = value;
        }
        else if (target.TryGetPropertyValue("id", out _))
        {
            entityJson["id"] = value;
        }
        else if (target.TryGetPropertyValue("tags", out _))
        {
            entityJson["tags"] = value;
        }
        else if (target.TryGetPropertyValue("transform", out var transformTarget))
        {
            if (transformTarget is not JsonObject transformTargetObj)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("Transform target must be an object like { 'position': 'x' }"));
                return;
            }

            if (transformTargetObj.Count != 1)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("Transform target must specify exactly one property"));
                return;
            }

            if (!entityJson.TryGetPropertyValue("transform", out var transformNode) || transformNode is not JsonObject transformObj)
            {
                transformObj = new JsonObject();
                entityJson["transform"] = transformObj;
            }
            else
            {
                transformObj = (JsonObject)transformNode;
            }

            // transformTarget is like { "position": "x" } - get the single property
            var (propName, propTarget) = transformTargetObj.First();
            
            if (!TryGetStringValue(propTarget, out var componentName))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("Transform property target must be a component name string (e.g., 'x', 'y', 'z', 'w')"));
                return;
            }

            // Get or create the property object
            if (!transformObj.TryGetPropertyValue(propName, out var existingPropNode) || existingPropNode is not JsonObject existingPropObj)
            {
                existingPropObj = new JsonObject();
                transformObj[propName] = existingPropObj;
            }
            else
            {
                existingPropObj = (JsonObject)existingPropNode;
            }

            existingPropObj[componentName] = value;
        }
        else if (target.TryGetPropertyValue("parent", out _))
        {
            entityJson["parent"] = value;
        }
        else if (target.TryGetPropertyValue("flexAlignItems", out _))
        {
            entityJson["flexAlignItems"] = value;
        }
        else if (target.TryGetPropertyValue("flexAlignSelf", out _))
        {
            entityJson["flexAlignSelf"] = value;
        }
        else if (target.TryGetPropertyValue("flexBorderBottom", out _))
        {
            entityJson["flexBorderBottom"] = value;
        }
        else if (target.TryGetPropertyValue("flexBorderLeft", out _))
        {
            entityJson["flexBorderLeft"] = value;
        }
        else if (target.TryGetPropertyValue("flexBorderRight", out _))
        {
            entityJson["flexBorderRight"] = value;
        }
        else if (target.TryGetPropertyValue("flexBorderTop", out _))
        {
            entityJson["flexBorderTop"] = value;
        }
        else if (target.TryGetPropertyValue("flexDirection", out _))
        {
            entityJson["flexDirection"] = value;
        }
        else if (target.TryGetPropertyValue("flexGrow", out _))
        {
            entityJson["flexGrow"] = value;
        }
        else if (target.TryGetPropertyValue("flexWrap", out _))
        {
            entityJson["flexWrap"] = value;
        }
        else if (target.TryGetPropertyValue("flexZOverride", out _))
        {
            entityJson["flexZOverride"] = value;
        }
        else if (target.TryGetPropertyValue("flexHeight", out var flexHeightTarget))
        {
            if (TryGetStringValue(flexHeightTarget, out var propName))
            {
                // Nested target like { "flexHeight": "value" } or { "flexHeight": "percent" }
                if (propName != "value" && propName != "percent")
                {
                    EventBus<ErrorEvent>.Push(new ErrorEvent("flexHeight target property must be 'value' or 'percent'"));
                    return;
                }

                if (propName == "value")
                {
                    entityJson["flexHeight"] = value;
                }
                else // percent
                {
                    entityJson["flexHeightPercent"] = value;
                }
            }
            else
            {
                // Simple target like { "flexHeight": true } - set value only
                entityJson["flexHeight"] = value;
            }
        }
        else if (target.TryGetPropertyValue("flexJustifyContent", out _))
        {
            entityJson["flexJustifyContent"] = value;
        }
        else if (target.TryGetPropertyValue("flexMarginBottom", out _))
        {
            entityJson["flexMarginBottom"] = value;
        }
        else if (target.TryGetPropertyValue("flexMarginLeft", out _))
        {
            entityJson["flexMarginLeft"] = value;
        }
        else if (target.TryGetPropertyValue("flexMarginRight", out _))
        {
            entityJson["flexMarginRight"] = value;
        }
        else if (target.TryGetPropertyValue("flexMarginTop", out _))
        {
            entityJson["flexMarginTop"] = value;
        }
        else if (target.TryGetPropertyValue("flexPaddingBottom", out _))
        {
            entityJson["flexPaddingBottom"] = value;
        }
        else if (target.TryGetPropertyValue("flexPaddingLeft", out _))
        {
            entityJson["flexPaddingLeft"] = value;
        }
        else if (target.TryGetPropertyValue("flexPaddingRight", out _))
        {
            entityJson["flexPaddingRight"] = value;
        }
        else if (target.TryGetPropertyValue("flexPaddingTop", out _))
        {
            entityJson["flexPaddingTop"] = value;
        }
        else if (target.TryGetPropertyValue("flexPositionBottom", out var flexPosBottomTarget))
        {
            if (TryGetStringValue(flexPosBottomTarget, out var propName))
            {
                if (propName != "value" && propName != "percent")
                {
                    EventBus<ErrorEvent>.Push(new ErrorEvent("flexPositionBottom target property must be 'value' or 'percent'"));
                    return;
                }

                if (propName == "value")
                {
                    entityJson["flexPositionBottom"] = value;
                }
                else
                {
                    entityJson["flexPositionBottomPercent"] = value;
                }
            }
            else
            {
                entityJson["flexPositionBottom"] = value;
            }
        }
        else if (target.TryGetPropertyValue("flexPositionLeft", out var flexPosLeftTarget))
        {
            if (TryGetStringValue(flexPosLeftTarget, out var propName))
            {
                if (propName != "value" && propName != "percent")
                {
                    EventBus<ErrorEvent>.Push(new ErrorEvent("flexPositionLeft target property must be 'value' or 'percent'"));
                    return;
                }

                if (propName == "value")
                {
                    entityJson["flexPositionLeft"] = value;
                }
                else
                {
                    entityJson["flexPositionLeftPercent"] = value;
                }
            }
            else
            {
                entityJson["flexPositionLeft"] = value;
            }
        }
        else if (target.TryGetPropertyValue("flexPositionRight", out var flexPosRightTarget))
        {
            if (TryGetStringValue(flexPosRightTarget, out var propName))
            {
                if (propName != "value" && propName != "percent")
                {
                    EventBus<ErrorEvent>.Push(new ErrorEvent("flexPositionRight target property must be 'value' or 'percent'"));
                    return;
                }

                if (propName == "value")
                {
                    entityJson["flexPositionRight"] = value;
                }
                else
                {
                    entityJson["flexPositionRightPercent"] = value;
                }
            }
            else
            {
                entityJson["flexPositionRight"] = value;
            }
        }
        else if (target.TryGetPropertyValue("flexPositionTop", out var flexPosTopTarget))
        {
            if (TryGetStringValue(flexPosTopTarget, out var propName))
            {
                if (propName != "value" && propName != "percent")
                {
                    EventBus<ErrorEvent>.Push(new ErrorEvent("flexPositionTop target property must be 'value' or 'percent'"));
                    return;
                }

                if (propName == "value")
                {
                    entityJson["flexPositionTop"] = value;
                }
                else
                {
                    entityJson["flexPositionTopPercent"] = value;
                }
            }
            else
            {
                entityJson["flexPositionTop"] = value;
            }
        }
        else if (target.TryGetPropertyValue("flexPositionType", out _))
        {
            entityJson["flexPositionType"] = value;
        }
        else if (target.TryGetPropertyValue("flexWidth", out var flexWidthTarget))
        {
            if (TryGetStringValue(flexWidthTarget, out var propName))
            {
                // Nested target like { "flexWidth": "value" } or { "flexWidth": "percent" }
                if (propName != "value" && propName != "percent")
                {
                    EventBus<ErrorEvent>.Push(new ErrorEvent("flexWidth target property must be 'value' or 'percent'"));
                    return;
                }

                if (propName == "value")
                {
                    entityJson["flexWidth"] = value;
                }
                else // percent
                {
                    entityJson["flexWidthPercent"] = value;
                }
            }
            else
            {
                // Simple target like { "flexWidth": true } - set value only
                entityJson["flexWidth"] = value;
            }
        }
        else
        {
            var validTargets = string.Join(", ", ValidTargets);
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Unrecognized target. Expected one of: {validTargets}"));
        }
    }

    private static bool TryApplyJsonLogic(JsonNode logic, JsonNode data, [NotNullWhen(true)] out JsonNode? result)
    {
        try
        {
            result = JsonLogic.Apply(logic, data);
            return result != null;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"JsonLogic evaluation failed: {ex.Message}"));
            result = null;
            return false;
        }
    }

    private static bool TryGetStringValue(JsonNode? node, [NotNullWhen(true)] out string? value)
    {
        if (node is not JsonValue jsonValue)
        {
            value = null;
            return false;
        }

        return jsonValue.TryGetValue(out value) && !string.IsNullOrWhiteSpace(value);
    }
}
