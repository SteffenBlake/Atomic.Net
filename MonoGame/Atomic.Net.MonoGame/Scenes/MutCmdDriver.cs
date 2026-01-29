using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
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
    /// <summary>
    /// Executes a MutCommand by applying all operations to the entity JsonNode.
    /// </summary>
    /// <param name="command">The mutation command to execute</param>
    /// <param name="context">The context JsonNode containing variables for JsonLogic evaluation</param>
    /// <param name="entityJson">The entity JsonNode to mutate in-place</param>
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
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Entity JSON is not a JsonObject"
            ));
            return;
        }

        if (!TryApplyJsonLogic(operation.Value, context, out var computedValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Failed to evaluate mutation value"
            ));
            return;
        }

        if (computedValue == null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Mutation value evaluation returned null"
            ));
            return;
        }

        if (operation.Target is not JsonObject targetObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Target is not a JsonObject"
            ));
            return;
        }

        ApplyToEntityJson(targetObj, computedValue, entityObj);
    }

    private static void ApplyToEntityJson(JsonObject target, JsonNode value, JsonObject entityJson)
    {
        if (target.TryGetPropertyValue("properties", out var propertyKeyNode) && propertyKeyNode != null)
        {
            if (!TryGetStringValue(propertyKeyNode, out var propertyKey))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("Failed to parse property key"));
                return;
            }

            if (!entityJson.ContainsKey("properties"))
            {
                entityJson["properties"] = new JsonObject();
            }

            var propertiesObj = entityJson["properties"]!.AsObject();
            propertiesObj[propertyKey] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("id", out _))
        {
            entityJson["id"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("tags", out _))
        {
            entityJson["tags"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("transform", out var transformField) && transformField != null)
        {
            if (!entityJson.ContainsKey("transform"))
            {
                entityJson["transform"] = new JsonObject();
            }

            var transformObj = entityJson["transform"]!.AsObject();
            if (transformField is JsonObject transformFieldObj)
            {
                foreach (var (key, _) in transformFieldObj)
                {
                    transformObj[key] = value.DeepClone();
                }
            }
        }
        else if (target.TryGetPropertyValue("parent", out _))
        {
            entityJson["parent"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexAlignItems", out _))
        {
            entityJson["flexAlignItems"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexAlignSelf", out _))
        {
            entityJson["flexAlignSelf"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexBorderBottom", out _))
        {
            entityJson["flexBorderBottom"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexBorderLeft", out _))
        {
            entityJson["flexBorderLeft"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexBorderRight", out _))
        {
            entityJson["flexBorderRight"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexBorderTop", out _))
        {
            entityJson["flexBorderTop"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexDirection", out _))
        {
            entityJson["flexDirection"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexGrow", out _))
        {
            entityJson["flexGrow"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexWrap", out _))
        {
            entityJson["flexWrap"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexZOverride", out _))
        {
            entityJson["flexZOverride"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexHeight", out _))
        {
            entityJson["flexHeight"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexJustifyContent", out _))
        {
            entityJson["flexJustifyContent"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexMarginBottom", out _))
        {
            entityJson["flexMarginBottom"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexMarginLeft", out _))
        {
            entityJson["flexMarginLeft"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexMarginRight", out _))
        {
            entityJson["flexMarginRight"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexMarginTop", out _))
        {
            entityJson["flexMarginTop"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexPaddingBottom", out _))
        {
            entityJson["flexPaddingBottom"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexPaddingLeft", out _))
        {
            entityJson["flexPaddingLeft"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexPaddingRight", out _))
        {
            entityJson["flexPaddingRight"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexPaddingTop", out _))
        {
            entityJson["flexPaddingTop"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexPositionBottom", out _))
        {
            entityJson["flexPositionBottom"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexPositionLeft", out _))
        {
            entityJson["flexPositionLeft"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexPositionRight", out _))
        {
            entityJson["flexPositionRight"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexPositionTop", out _))
        {
            entityJson["flexPositionTop"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexPositionType", out _))
        {
            entityJson["flexPositionType"] = value.DeepClone();
        }
        else if (target.TryGetPropertyValue("flexWidth", out _))
        {
            entityJson["flexWidth"] = value.DeepClone();
        }
        else
        {
            var validTargets = string.Join(", ", new[]
            {
                "properties", "id", "tags", "transform", "parent",
                "flexAlignItems", "flexAlignSelf",
                "flexBorderBottom", "flexBorderLeft", "flexBorderRight", "flexBorderTop",
                "flexDirection", "flexGrow", "flexWrap", "flexZOverride",
                "flexHeight", "flexJustifyContent",
                "flexMarginBottom", "flexMarginLeft", "flexMarginRight", "flexMarginTop",
                "flexPaddingBottom", "flexPaddingLeft", "flexPaddingRight", "flexPaddingTop",
                "flexPositionBottom", "flexPositionLeft", "flexPositionRight", "flexPositionTop", "flexPositionType",
                "flexWidth"
            });
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Unrecognized target. Expected one of: {validTargets}"
            ));
        }
    }

    private static bool TryApplyJsonLogic(
        JsonNode? rule,
        JsonNode? data,
        [NotNullWhen(true)]
        out JsonNode? result
    )
    {
        if (rule == null)
        {
            result = null;
            return false;
        }

        try
        {
            var jsonLogicRule = JsonSerializer.Deserialize<Rule>(rule.ToJsonString());
            if (jsonLogicRule == null)
            {
                result = null;
                return false;
            }

            result = jsonLogicRule.Apply(data);
            return result != null;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"JsonLogic evaluation failed: {ex.Message}"));
            result = null;
            return false;
        }
    }

    private static bool TryGetStringValue(
        JsonNode node,
        [NotNullWhen(true)]
        out string? value
    )
    {
        if (node is not JsonValue jsonValue)
        {
            value = null;
            return false;
        }

        return jsonValue.TryGetValue(out value) && !string.IsNullOrWhiteSpace(value);
    }
}
