using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Microsoft.Xna.Framework;

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

    /// <summary>
    /// Tries to parse a Vector3 from a JsonObject with x/y/z properties.
    /// Missing properties use values from the default.
    /// </summary>
    public static bool TryGetVector3Value(
        this JsonObject obj,
        Vector3 defaultValue,
        out Vector3 result
    )
    {
        var x = defaultValue.X;
        var y = defaultValue.Y;
        var z = defaultValue.Z;
        var success = true;

        if (obj.TryGetPropertyValue("x", out var xNode) && xNode != null)
        {
            if (!xNode.TryGetFloatValue(out x))
            {
                success = false;
            }
        }

        if (obj.TryGetPropertyValue("y", out var yNode) && yNode != null)
        {
            if (!yNode.TryGetFloatValue(out y))
            {
                success = false;
            }
        }

        if (obj.TryGetPropertyValue("z", out var zNode) && zNode != null)
        {
            if (!zNode.TryGetFloatValue(out z))
            {
                success = false;
            }
        }

        result = new Vector3(x, y, z);
        return success;
    }

    /// <summary>
    /// Tries to parse a Quaternion from a JsonObject with x/y/z/w properties.
    /// Missing properties use values from the default.
    /// </summary>
    public static bool TryGetQuaternionValue(
        this JsonObject obj,
        Quaternion defaultValue,
        out Quaternion result
    )
    {
        var x = defaultValue.X;
        var y = defaultValue.Y;
        var z = defaultValue.Z;
        var w = defaultValue.W;
        var success = true;

        if (obj.TryGetPropertyValue("x", out var xNode) && xNode != null)
        {
            if (!xNode.TryGetFloatValue(out x))
            {
                success = false;
            }
        }

        if (obj.TryGetPropertyValue("y", out var yNode) && yNode != null)
        {
            if (!yNode.TryGetFloatValue(out y))
            {
                success = false;
            }
        }

        if (obj.TryGetPropertyValue("z", out var zNode) && zNode != null)
        {
            if (!zNode.TryGetFloatValue(out z))
            {
                success = false;
            }
        }

        if (obj.TryGetPropertyValue("w", out var wNode) && wNode != null)
        {
            if (!wNode.TryGetFloatValue(out w))
            {
                success = false;
            }
        }

        result = new Quaternion(x, y, z, w);
        return success;
    }
}
