using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for IJsonExpressionSymbolEquals.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionSymbolEqualsConverter<TIn, TOut> : JsonConverter<IJsonExpressionSymbolEquals<TIn, TOut>>
{
    public override IJsonExpressionSymbolEquals<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // Validate: TOut must be bool (equality operators always return boolean)
        if (typeof(TOut) != typeof(bool))
        {
            throw new JsonException(
                $"'==' operator always returns bool, but requested output type is '{typeof(TOut).Name}'"
            );
        }

        // Use the built-in fast path from Utf8JsonReader to JsonDocument
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Validate: must be an array (parent already parsed the "==" key)
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array of 2 arguments for '==' operator, Actual: {root.ValueKind}"
            );
        }

        // Validate: array must have exactly 2 elements
        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for '==' operator, Actual: {arrayLength} elements"
            );
        }

        // Deserialize left and right expressions with inferred types
        var leftElement = root[0];
        var rightElement = root[1];

        // Infer the actual types from the JSON structure
        var compareType = leftElement.InferJsonExpressionType<TIn>();

        // Construct the generic IJsonExpression<TIn, TCompare> type for deserializing operands
        var expressionType = typeof(IJsonExpression<,>).MakeGenericType(typeof(TIn), compareType);

        // Deserialize with the correct type
        var left = JsonSerializer.Deserialize(leftElement, expressionType, options) ??
            throw new JsonException(
                $"Expected: Valid expression for left operand of '==', Actual: null after deserialization"
            );

        var right = JsonSerializer.Deserialize(rightElement, expressionType, options) ??
            throw new JsonException(
                $"Expected: Valid expression for right operand of '==', Actual: null after deserialization"
            );

        // Construct JsonExpressionSymbolEquals<TIn, TCompare, TOut> dynamically using reflection
        var equalsType = typeof(JsonExpressionSymbolEquals<,,>).MakeGenericType(typeof(TIn), compareType, typeof(TOut));
        var constructor = equalsType.GetConstructor([expressionType, expressionType]) ?? 
            throw new JsonException(
                $"Unable to find constructor for JsonExpressionSymbolEquals<{typeof(TIn).Name}, {compareType.Name}, {typeof(TOut).Name}>"
            );

        var result = constructor.Invoke([left, right]);
        return (IJsonExpressionSymbolEquals<TIn, TOut>)result;
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionSymbolEquals<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
