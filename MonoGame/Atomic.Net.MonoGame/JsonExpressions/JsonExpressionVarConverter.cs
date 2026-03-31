using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionVar.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionVarConverter<TIn, TOut> : JsonConverter<JsonExpressionVar<TIn, TOut>>
{
    public override JsonExpressionVar<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Var can be: "PropertyName" or ["PropertyName"] or ["PropertyName", defaultValue]
        if (root.ValueKind == JsonValueKind.String)
        {
            // Simple string property path
            var pathString = root.GetString();
            return new JsonExpressionVar<TIn, TOut>(pathString, null);
        }

        if (root.ValueKind == JsonValueKind.Number)
        {
            // Numeric index for arrays
            var index = root.GetInt32();
            return new JsonExpressionVar<TIn, TOut>(index.ToString(), null);
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

        // First element is the path
        string? path = null;
        if (root[0].ValueKind == JsonValueKind.String)
        {
            path = root[0].GetString();
        }
        else if (root[0].ValueKind == JsonValueKind.Number)
        {
            path = root[0].GetInt32().ToString();
        }
        else
        {
            throw new JsonException(
                $"Expected: String or Number for var path, Actual: {root[0].ValueKind}"
            );
        }

        // Optional second element is the default value
        JsonExpression<TIn, TOut>? defaultValue = null;
        if (arrayLength == 2)
        {
            defaultValue = JsonSerializer.Deserialize<JsonExpression<TIn, TOut>>(root[1], options);
            if (defaultValue is null)
            {
                throw new JsonException(
                    $"Expected: Valid expression for var default value, Actual: null"
                );
            }
        }

        return new JsonExpressionVar<TIn, TOut>(path, defaultValue);
    }

    public override void Write(
        Utf8JsonWriter writer,
        JsonExpressionVar<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
