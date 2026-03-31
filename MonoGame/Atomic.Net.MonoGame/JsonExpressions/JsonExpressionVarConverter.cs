using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionVar.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionVarConverter<TIn, TOut> : JsonConverter<IJsonExpressionVar<TIn, TOut>>
{
    public override IJsonExpressionVar<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Number)
        {
            // Numeric index: {"var": 1} → array element access
            var index = root.GetInt32();
            return new JsonExpressionVar<TIn, TOut>(null, index, null);
        }

        if (root.ValueKind == JsonValueKind.String)
        {
            // Simple string path: {"var": "A"} or {"var": ""} (identity)
            return new JsonExpressionVar<TIn, TOut>(ParsePath(root.GetString()), null, null);
        }

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: String, Number, or Array for var operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength is < 1 or > 2)
        {
            throw new JsonException(
                $"Expected: Array with 1 or 2 elements for var operator, Actual: Array with {arrayLength} elements"
            );
        }

        // First element is the path/index
        var firstElement = root[0];

        if (firstElement.ValueKind == JsonValueKind.Number)
        {
            var index = firstElement.GetInt32();
            return new JsonExpressionVar<TIn, TOut>(null, index, null);
        }

        if (firstElement.ValueKind != JsonValueKind.String)
        {
            throw new JsonException(
                $"Expected: String or Number for var path, Actual: {firstElement.ValueKind}"
            );
        }

        var properties = ParsePath(firstElement.GetString());

        // Optional second element is the default value
        IJsonExpression<TIn, TOut>? defaultValue = null;
        if (arrayLength == 2)
        {
            defaultValue = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(root[1], options) ??
                throw new JsonException(
                    $"Expected: Valid expression for var default value, Actual: null"
                );
        }

        return new JsonExpressionVar<TIn, TOut>(properties, null, defaultValue);
    }

    /// <summary>
    /// Parses a var path string into pre-split property segments.
    /// Returns null for empty paths (identity — returns entire input).
    /// </summary>
    private static string[]? ParsePath(string? path) =>
        string.IsNullOrEmpty(path) ? null : path.Split('.');

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionVar<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
