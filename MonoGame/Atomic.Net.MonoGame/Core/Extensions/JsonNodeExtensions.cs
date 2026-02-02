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
    /// Tries to coerce a float value from a JsonNode.
    /// Supports conversion from double, int, and decimal.
    /// Caller must ensure node is not null.
    /// </summary>
    public static bool TryCoerceFloatValue(this JsonNode node, [NotNullWhen(true)] out float? result)
    {
        if (node is not JsonValue jsonValue)
        {
            result = default;
            return false;
        }

        if (jsonValue.TryGetValue<float>(out var floatVal))
        {
            result = floatVal;
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
    /// Tries to parse a Vector3 from a JsonObject with x/y/z properties.
    /// Missing properties use values from the default.
    /// Returns false if any property fails to parse, but always outputs a result using defaults for missing/invalid fields.
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

        if (obj.TryGetChild("x", out var xNode))
        {
            if (xNode.TryCoerceFloatValue(out var xValue))
            {
                x = xValue.Value;
            }
            else
            {
                success = false;
            }
        }

        if (obj.TryGetChild("y", out var yNode))
        {
            if (yNode.TryCoerceFloatValue(out var yValue))
            {
                y = yValue.Value;
            }
            else
            {
                success = false;
            }
        }

        if (obj.TryGetChild("z", out var zNode))
        {
            if (zNode.TryCoerceFloatValue(out var zValue))
            {
                z = zValue.Value;
            }
            else
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
    /// Returns false if any property fails to parse, but always outputs a result using defaults for missing/invalid fields.
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

        if (obj.TryGetChild("x", out var xNode))
        {
            if (xNode.TryCoerceFloatValue(out var xValue))
            {
                x = xValue.Value;
            }
            else
            {
                success = false;
            }
        }

        if (obj.TryGetChild("y", out var yNode))
        {
            if (yNode.TryCoerceFloatValue(out var yValue))
            {
                y = yValue.Value;
            }
            else
            {
                success = false;
            }
        }

        if (obj.TryGetChild("z", out var zNode))
        {
            if (zNode.TryCoerceFloatValue(out var zValue))
            {
                z = zValue.Value;
            }
            else
            {
                success = false;
            }
        }

        if (obj.TryGetChild("w", out var wNode))
        {
            if (wNode.TryCoerceFloatValue(out var wValue))
            {
                w = wValue.Value;
            }
            else
            {
                success = false;
            }
        }

        result = new Quaternion(x, y, z, w);
        return success;
    }

    /// <summary>
    /// Tries to extract the _index property from entity JSON as a global partition index.
    /// Use this when you know the entity is from the global partition.
    /// </summary>
    public static bool TryGetGlobalEntityIndex(
        this JsonNode entityJson,
        [NotNullWhen(true)] out PartitionIndex? entityIndex
    )
    {
        if (entityJson is not JsonObject entityObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Entity JSON is not a JsonObject"));
            entityIndex = null;
            return false;
        }

        if (!entityObj.TryGetPropertyValue("_index", out var indexNode) || indexNode == null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Entity missing _index property"));
            entityIndex = null;
            return false;
        }

        if (indexNode is not JsonValue jsonValue)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Entity _index must be a numeric value"));
            entityIndex = null;
            return false;
        }

        // Global partition uses ushort indices
        if (!jsonValue.TryGetValue<ushort>(out var ushortValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Entity _index could not be parsed as ushort (global partition)"));
            entityIndex = null;
            return false;
        }

        if (ushortValue >= Constants.MaxGlobalEntities)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Entity _index {ushortValue} exceeds MaxGlobalEntities ({Constants.MaxGlobalEntities})"
            ));
            entityIndex = null;
            return false;
        }

        entityIndex = ushortValue;
        return true;
    }

    /// <summary>
    /// Tries to extract the _index property from entity JSON as a scene partition index.
    /// Use this when you know the entity is from the scene partition.
    /// </summary>
    public static bool TryGetSceneEntityIndex(
        this JsonNode entityJson,
        [NotNullWhen(true)] out PartitionIndex? entityIndex
    )
    {
        if (entityJson is not JsonObject entityObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Entity JSON is not a JsonObject"));
            entityIndex = null;
            return false;
        }

        if (!entityObj.TryGetPropertyValue("_index", out var indexNode) || indexNode == null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Entity missing _index property"));
            entityIndex = null;
            return false;
        }

        if (indexNode is not JsonValue jsonValue)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Entity _index must be a numeric value"));
            entityIndex = null;
            return false;
        }

        if (!jsonValue.TryGetValue<uint>(out var uintValue))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Entity _index could not be parsed as uint (scene partition)"));
            entityIndex = null;
            return false;
        }

        if (uintValue >= Constants.MaxSceneEntities)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Entity _index {uintValue} exceeds MaxSceneEntities ({Constants.MaxSceneEntities})"
            ));
            entityIndex = null;
            return false;
        }

        entityIndex = uintValue;
        return true;
    }
}
