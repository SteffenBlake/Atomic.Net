using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Atomic.Net.MonoGame.Core.Extensions;

/// <summary>
/// Extension methods for JsonNode to simplify value extraction and parsing.
/// </summary>
public static class JsonNodeExtensions
{
    /// <summary>
    /// Tries to get a child property from a JsonObject, ensuring it exists and is not null.
    /// </summary>
    public static bool TryGetChild(
        this JsonObject obj,
        string propertyName,
        [NotNullWhen(true)]
        out JsonNode? child
    )
    {
        if (obj.TryGetPropertyValue(propertyName, out child) && child != null)
        {
            return true;
        }

        child = null;
        return false;
    }

    /// <summary>
    /// Tries to get a string value from a JsonNode.
    /// Caller must ensure node is not null.
    /// </summary>
    public static bool TryGetStringValue(
        this JsonNode node,
        [NotNullWhen(true)]
        out string? result
    )
    {
        // Use JsonValue.TryGetValue instead of try/catch
        if (node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out result))
        {
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Tries to get a float value from a JsonNode.
    /// Supports conversion from double, int, and decimal.
    /// Caller must ensure node is not null.
    /// </summary>
    public static bool TryGetFloatValue(this JsonNode node, out float result)
    {
        if (node is not JsonValue jsonValue)
        {
            result = default;
            return false;
        }

        if (jsonValue.TryGetValue<float>(out result))
        {
            return true;
        }

        if (jsonValue.TryGetValue<double>(out var doubleVal))
        {
            result = (float)doubleVal;
            return true;
        }

        if (jsonValue.TryGetValue<int>(out var intVal))
        {
            result = intVal;
            return true;
        }

        if (jsonValue.TryGetValue<decimal>(out var decimalVal))
        {
            result = (float)decimalVal;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Tries to get a bool value from a JsonNode.
    /// Caller must ensure node is not null.
    /// </summary>
    public static bool TryGetBoolValue(this JsonNode node, out bool result)
    {
        if (node is not JsonValue jsonValue)
        {
            result = default;
            return false;
        }

        if (jsonValue.TryGetValue<bool>(out result))
        {
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Tries to get an int value from a JsonNode.
    /// Caller must ensure node is not null.
    /// </summary>
    public static bool TryGetIntValue(this JsonNode node, out int result)
    {
        if (node is not JsonValue jsonValue)
        {
            result = default;
            return false;
        }

        if (jsonValue.TryGetValue<int>(out result))
        {
            return true;
        }

        result = default;
        return false;
    }
}
