using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionGreaterThan.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionGreaterThanConverter<TIn, TOut> : JsonConverter<JsonExpressionGreaterThan<TIn, TOut>>
{
    public override JsonExpressionGreaterThan<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // Validate: TOut must be bool (comparison operators return boolean)
        if (typeof(TOut) != typeof(bool))
        {
            throw new JsonException(
                $"Expected: TOut type 'bool' for '>' operator, Actual: '{typeof(TOut).Name}'"
            );
        }

        // Use the built-in fast path from Utf8JsonReader to JsonDocument
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Validate: must be an array (parent already parsed the ">" key)
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array of 2 arguments for '>' operator, Actual: {root.ValueKind}"
            );
        }

        // Validate: array must have exactly 2 elements
        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for '>' operator, Actual: {arrayLength} elements"
            );
        }

        // Deserialize left and right expressions as float (comparison operands are numbers)
        var leftElement = root[0];
        var rightElement = root[1];

        var left = JsonSerializer.Deserialize<JsonExpression<TIn, float>>(leftElement, options);
        var right = JsonSerializer.Deserialize<JsonExpression<TIn, float>>(rightElement, options);

        if (left is null)
        {
            throw new JsonException(
                $"Expected: Valid expression for left operand of '>', Actual: null after deserialization"
            );
        }

        if (right is null)
        {
            throw new JsonException(
                $"Expected: Valid expression for right operand of '>', Actual: null after deserialization"
            );
        }

        return new JsonExpressionGreaterThan<TIn, TOut>(left, right);
    }

    public override void Write(
        Utf8JsonWriter writer,
        JsonExpressionGreaterThan<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
