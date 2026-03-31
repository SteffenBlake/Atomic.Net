using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionSymbolLessThan.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionSymbolLessThanConverter<TIn, TOut> : JsonConverter<IJsonExpressionSymbolLessThan<TIn, TOut>>
{
    public override IJsonExpressionSymbolLessThan<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // Validate: TOut must be bool (comparison operators return boolean)
        if (typeof(TOut) != typeof(bool))
        {
            throw new JsonException(
                $"Unable to convert 'bool' operation of '<' operator to requested type of '{typeof(TOut).Name}'"
            );
        }

        // Use the built-in fast path from Utf8JsonReader to JsonDocument
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Validate: must be an array (parent already parsed the "<" key)
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array of 2 arguments for '<' operator, Actual: {root.ValueKind}"
            );
        }

        // Validate: array must have exactly 2 elements
        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for '<' operator, Actual: {arrayLength} elements"
            );
        }

        // Deserialize left and right expressions as float (comparison operands are numbers)
        var leftElement = root[0];
        var rightElement = root[1];

        var left = JsonSerializer.Deserialize<IJsonExpression<TIn, float>>(leftElement, options) ??
            throw new JsonException(
                $"Expected: Valid expression for left operand of '<', Actual: null after deserialization"
            );

        var right = JsonSerializer.Deserialize<IJsonExpression<TIn, float>>(rightElement, options) ??
            throw new JsonException(
                $"Expected: Valid expression for right operand of '<', Actual: null after deserialization"
            );

        return new JsonExpressionSymbolLessThan<TIn, float, TOut>(left, right);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionSymbolLessThan<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
