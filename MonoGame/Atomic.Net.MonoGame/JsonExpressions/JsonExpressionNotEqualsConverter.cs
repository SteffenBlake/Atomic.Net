using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionNotEquals.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionNotEqualsConverter<TIn, TOut> : JsonConverter<JsonExpressionNotEquals<TIn, TOut>>
{
    public override JsonExpressionNotEquals<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // Validate: TOut must be bool (comparison operators return boolean)
        if (typeof(TOut) != typeof(bool))
        {
            throw new JsonException(
                $"Unable to convert 'bool' operation of '!=' operator to requested type of '{typeof(TOut).Name}'"
            );
        }

        // Use the built-in fast path from Utf8JsonReader to JsonDocument
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Validate: must be an array (parent already parsed the "!=" key)
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array of 2 arguments for '!=' operator, Actual: {root.ValueKind}"
            );
        }

        // Validate: array must have exactly 2 elements
        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for '!=' operator, Actual: {arrayLength} elements"
            );
        }

        // Deserialize left and right expressions as generic TOut (inequality works with any type)
        var leftElement = root[0];
        var rightElement = root[1];

        var left = JsonSerializer.Deserialize<JsonExpression<TIn, TOut>>(leftElement, options);
        var right = JsonSerializer.Deserialize<JsonExpression<TIn, TOut>>(rightElement, options);

        if (left is null)
        {
            throw new JsonException(
                $"Expected: Valid expression for left operand of '!=', Actual: null after deserialization"
            );
        }

        if (right is null)
        {
            throw new JsonException(
                $"Expected: Valid expression for right operand of '!=', Actual: null after deserialization"
            );
        }

        return new JsonExpressionNotEquals<TIn, TOut>(left, right);
    }

    public override void Write(
        Utf8JsonWriter writer,
        JsonExpressionNotEquals<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
