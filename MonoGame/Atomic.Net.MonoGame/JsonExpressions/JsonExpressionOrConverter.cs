using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionOr.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionOrConverter<TIn, TOut> : JsonConverter<IJsonExpressionOr<TIn, TOut>>
{
    public override IJsonExpressionOr<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (typeof(TOut) != typeof(bool))
        {
            throw new JsonException(
                $"'or' operator requires TOut=bool, got TOut={typeof(TOut).Name}"
            );
        }

        // Use the built-in fast path from Utf8JsonReader to JsonDocument
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Validate: must be an array (parent already parsed the "or" key)
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array of operands for 'or' operator, Actual: {root.ValueKind}"
            );
        }

        // Validate: array must have at least 1 element
        var arrayLength = root.GetArrayLength();
        if (arrayLength == 0)
        {
            throw new JsonException(
                $"Expected: Array with at least 1 element for 'or' operator, Actual: 0 elements"
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
                    $"Expected: Valid expression for operand {i} of 'or', Actual: null after deserialization"
                );
            }
            operands[i] = operand;
        }

        return new JsonExpressionOr<TIn, TOut>(operands);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionOr<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
