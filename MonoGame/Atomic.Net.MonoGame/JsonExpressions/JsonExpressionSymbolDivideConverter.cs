using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionSymbolDivide.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionSymbolDivideConverter<TIn, TOut> : JsonConverter<IJsonExpressionSymbolDivide<TIn, TOut>>
{
    public override IJsonExpressionSymbolDivide<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (typeof(TOut) == typeof(bool) || typeof(TOut) == typeof(string) || typeof(TOut).IsArray)
        {
            throw new JsonException(
                $"'/' operator requires a numeric TOut, got TOut={typeof(TOut).Name}"
            );
        }

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

        var dividend = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(dividendElement, options);
        var divisor = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(divisorElement, options);

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

        return new JsonExpressionSymbolDivide<TIn, TOut>(dividend, divisor);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionSymbolDivide<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
