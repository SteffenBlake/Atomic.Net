using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Core.Extensions;

/// <summary>
/// Extension methods for JsonObject to simplify parsing and validation.
/// </summary>
public static class JsonObjectExtensions
{
    /// <summary>
    /// Tries to parse an enum value from a JsonNode string.
    /// Fires ErrorEvent on failure with descriptive message.
    /// Caller must ensure value is not null.
    /// </summary>
    public static bool TryGetEnumWithError<TEnum>(
        this JsonNode value, 
        ushort entityIndex, 
        string fieldName, 
        out TEnum result
    )
        where TEnum : struct, Enum
    {
        result = default;

        // Use TryGetStringValue instead of try/catch
        if (!value.TryGetStringValue(out var stringValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} for entity {entityIndex}: Expected string value"
            ));
            return false;
        }

        if (!Enum.TryParse<TEnum>(stringValue, ignoreCase: true, out result))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Invalid {fieldName} value '{stringValue}' for entity {entityIndex}. Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}"
            ));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Tries to parse a two-field behavior (value + percent) from a JsonNode.
    /// Expects JsonObject with "value" and "percent" fields.
    /// Fires ErrorEvent on failure with descriptive message.
    /// Caller must ensure value is not null.
    /// </summary>
    public static bool TryParseTwoFieldBehavior(
        this JsonNode value, 
        ushort entityIndex, 
        string fieldName, 
        out float floatValue, 
        out bool percentValue
    )
    {
        floatValue = default;
        percentValue = default;
        
        if (value is not JsonObject obj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} for entity {entityIndex}: Expected JsonObject with 'value' and 'percent' fields"
            ));
            return false;
        }

        if (!obj.TryGetChild("value", out var valueNode) || 
            !valueNode.TryCoerceFloatValue(out var floatVal))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName}.value for entity {entityIndex}: Expected numeric 'value' field"
            ));
            return false;
        }

        floatValue = floatVal.Value;

        if (!obj.TryGetChild("percent", out var percentNode) || 
            !percentNode.TryCoerceBoolValue(out var boolVal))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName}.percent for entity {entityIndex}: Expected boolean 'percent' field"
            ));
            return false;
        }

        percentValue = boolVal.Value;

        return true;
    }
}
