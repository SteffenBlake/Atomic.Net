using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionSymbolMultiply.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionSymbolMultiplyConverter<TIn, TOut> : JsonConverter<IJsonExpressionSymbolMultiply<TIn, TOut>>
{
    public override IJsonExpressionSymbolMultiply<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (typeof(TOut) == typeof(bool) || typeof(TOut) == typeof(string) || typeof(TOut).IsArray)
        {
            throw new JsonException(
                $"'*' operator requires a numeric TOut, got TOut={typeof(TOut).Name}"
            );
        }

        // Use the built-in fast path from Utf8JsonReader to JsonDocument
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Validate: must be an array (parent already parsed the "*" key)
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array of 2 arguments for '*' operator, Actual: {root.ValueKind}"
            );
        }

        // Validate: array must have exactly 2 elements
        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for '*' operator, Actual: {arrayLength} elements"
            );
        }

        // Deserialize left and right expressions
        var leftElement = root[0];
        var rightElement = root[1];

        var left = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(leftElement, options);
        var right = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(rightElement, options);

        if (left is null)
        {
            throw new JsonException(
                $"Expected: Valid expression for left operand of '*', Actual: null after deserialization"
            );
        }

        if (right is null)
        {
            throw new JsonException(
                $"Expected: Valid expression for right operand of '*', Actual: null after deserialization"
            );
        }

        return new JsonExpressionSymbolMultiply<TIn, TOut>(left, right);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionSymbolMultiply<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
