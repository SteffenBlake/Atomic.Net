using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionSymbolSubtract.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionSymbolSubtractConverter<TIn, TOut> : JsonConverter<IJsonExpressionSymbolSubtract<TIn, TOut>>
{
    public override IJsonExpressionSymbolSubtract<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // Use the built-in fast path from Utf8JsonReader to JsonDocument
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Validate: must be an array (parent already parsed the "-" key)
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array of 2 arguments for '-' operator, Actual: {root.ValueKind}"
            );
        }

        // Validate: array must have exactly 2 elements
        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for '-' operator, Actual: {arrayLength} elements"
            );
        }

        // Deserialize minuend and subtrahend expressions
        var minuendElement = root[0];
        var subtrahendElement = root[1];

        var minuend = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(minuendElement, options);
        var subtrahend = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(subtrahendElement, options);

        if (minuend is null)
        {
            throw new JsonException(
                $"Expected: Valid expression for minuend of '-', Actual: null after deserialization"
            );
        }

        if (subtrahend is null)
        {
            throw new JsonException(
                $"Expected: Valid expression for subtrahend of '-', Actual: null after deserialization"
            );
        }

        return new JsonExpressionSymbolSubtract<TIn, TOut>(minuend, subtrahend);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionSymbolSubtract<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
