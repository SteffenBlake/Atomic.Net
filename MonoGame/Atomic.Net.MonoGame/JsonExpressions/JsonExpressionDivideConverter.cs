using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionDivide.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionDivideConverter<TIn, TOut> : JsonConverter<JsonExpressionDivide<TIn, TOut>>
{
    public override JsonExpressionDivide<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // Use the built-in fast path from Utf8JsonReader to JsonDocument
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Validate: must be an array (parent already parsed the "/" key)
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array of 2 arguments for '/' operator, Actual: {root.ValueKind}"
            );
        }

        // Validate: array must have exactly 2 elements
        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for '/' operator, Actual: {arrayLength} elements"
            );
        }

        // Deserialize dividend and divisor expressions
        var dividendElement = root[0];
        var divisorElement = root[1];

        var dividend = JsonSerializer.Deserialize<JsonExpression<TIn, TOut>>(dividendElement, options);
        var divisor = JsonSerializer.Deserialize<JsonExpression<TIn, TOut>>(divisorElement, options);

        if (dividend is null)
        {
            throw new JsonException(
                $"Expected: Valid expression for dividend of '/', Actual: null after deserialization"
            );
        }

        if (divisor is null)
        {
            throw new JsonException(
                $"Expected: Valid expression for divisor of '/', Actual: null after deserialization"
            );
        }

        return new JsonExpressionDivide<TIn, TOut>(dividend, divisor);
    }

    public override void Write(
        Utf8JsonWriter writer,
        JsonExpressionDivide<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
