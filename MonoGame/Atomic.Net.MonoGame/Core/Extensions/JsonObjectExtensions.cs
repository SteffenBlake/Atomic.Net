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
    /// </summary>
    public static bool TryParseEnum<TEnum>(
        this JsonNode? value, 
        ushort entityIndex, 
        string fieldName, 
        out TEnum result
    )
        where TEnum : struct, Enum
    {
        result = default;
        
        if (value == null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} for entity {entityIndex}: value is null"
            ));
            return false;
        }

        try
        {
            var stringValue = value.GetValue<string>();
            if (Enum.TryParse<TEnum>(stringValue, ignoreCase: true, out result))
            {
                return true;
            }

            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Invalid {fieldName} value '{stringValue}' for entity {entityIndex}. Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}"
            ));
            return false;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} for entity {entityIndex}: {ex.Message}"
            ));
            return false;
        }
    }

    /// <summary>
    /// Tries to parse a float value from a JsonNode.
    /// Fires ErrorEvent on failure with descriptive message.
    /// </summary>
    public static bool TryParseFloat(
        this JsonNode? value, 
        ushort entityIndex, 
        string fieldName, 
        out float result
    )
    {
        result = default;
        
        if (value == null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} for entity {entityIndex}: value is null"
            ));
            return false;
        }

        try
        {
            result = value.GetValue<float>();
            return true;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} for entity {entityIndex}: {ex.Message}. Expected a numeric value."
            ));
            return false;
        }
    }

    /// <summary>
    /// Tries to parse an int value from a JsonNode.
    /// Fires ErrorEvent on failure with descriptive message.
    /// </summary>
    public static bool TryParseInt(
        this JsonNode? value, 
        ushort entityIndex, 
        string fieldName, 
        out int result
    )
    {
        result = default;
        
        if (value == null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} for entity {entityIndex}: value is null"
            ));
            return false;
        }

        try
        {
            result = value.GetValue<int>();
            return true;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName} for entity {entityIndex}: {ex.Message}. Expected an integer value."
            ));
            return false;
        }
    }

    /// <summary>
    /// Tries to parse a two-field behavior (value + percent) from a JsonNode.
    /// Expects JsonObject with "value" and "percent" fields.
    /// Fires ErrorEvent on failure with descriptive message.
    /// </summary>
    public static bool TryParseTwoFieldBehavior(
        this JsonNode? value, 
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

        if (!obj.TryGetPropertyValue("value", out var valueNode) || 
            !valueNode.TryGetFloatValue(out floatValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName}.value for entity {entityIndex}: Expected numeric 'value' field"
            ));
            return false;
        }

        if (!obj.TryGetPropertyValue("percent", out var percentNode) || 
            !percentNode.TryGetBoolValue(out percentValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse {fieldName}.percent for entity {entityIndex}: Expected boolean 'percent' field"
            ));
            return false;
        }

        return true;
    }
}
