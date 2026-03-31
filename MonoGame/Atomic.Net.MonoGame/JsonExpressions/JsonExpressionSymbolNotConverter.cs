using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionSymbolNot.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionSymbolNotConverter<TIn, TOut> : JsonConverter<IJsonExpressionSymbolNot<TIn, TOut>>
{
    public override IJsonExpressionSymbolNot<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // Use the built-in fast path from Utf8JsonReader to JsonDocument
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        JsonElement operandElement;

        // Not can be either a single value or an array with one element
        if (root.ValueKind == JsonValueKind.Array)
        {
            // Array format: {"!": [value]}
            var arrayLength = root.GetArrayLength();
            if (arrayLength != 1)
            {
                throw new JsonException(
                    $"Expected: Array with 1 element for '!' operator, Actual: {arrayLength} elements"
                );
            }
            operandElement = root[0];
        }
        else
        {
            // Direct value format: {"!": value}
            operandElement = root;
        }

        // Deserialize the operand as bool expression
        var operand = JsonSerializer.Deserialize<IJsonExpression<TIn, bool>>(operandElement, options);
        if (operand is null)
        {
            throw new JsonException(
                $"Expected: Valid expression for operand of '!', Actual: null after deserialization"
            );
        }

        return new JsonExpressionSymbolNot<TIn, TOut>(operand);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionSymbolNot<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
