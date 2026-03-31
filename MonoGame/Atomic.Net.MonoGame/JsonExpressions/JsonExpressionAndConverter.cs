using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionAnd.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionAndConverter<TIn, TOut> : JsonConverter<IJsonExpressionAnd<TIn, TOut>>
{
    public override IJsonExpressionAnd<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // Use the built-in fast path from Utf8JsonReader to JsonDocument
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Validate: must be an array (parent already parsed the "and" key)
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array of operands for 'and' operator, Actual: {root.ValueKind}"
            );
        }

        // Validate: array must have at least 1 element
        var arrayLength = root.GetArrayLength();
        if (arrayLength == 0)
        {
            throw new JsonException(
                $"Expected: Array with at least 1 element for 'and' operator, Actual: 0 elements"
            );
        }

        // Deserialize all operands
        var operands = new IJsonExpression<TIn, TOut>[arrayLength];
        for (int i = 0; i < arrayLength; i++)
        {
            var operand = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(root[i], options);
            if (operand is null)
            {
                throw new JsonException(
                    $"Expected: Valid expression for operand {i} of 'and', Actual: null after deserialization"
                );
            }
            operands[i] = operand;
        }

        return new JsonExpressionAnd<TIn, TOut>(operands);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionAnd<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
