using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for IJsonExpressionSymbolNotEquals.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionSymbolNotEqualsConverter<TIn, TOut> : JsonConverter<IJsonExpressionSymbolNotEquals<TIn, TOut>>
{
    public override IJsonExpressionSymbolNotEquals<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // Validate: TOut must be bool (inequality operators always return boolean)
        if (typeof(TOut) != typeof(bool))
        {
            throw new JsonException(
                $"'!=' operator always returns bool, but requested output type is '{typeof(TOut).Name}'"
            );
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array of 2 arguments for '!=' operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for '!=' operator, Actual: {arrayLength} elements"
            );
        }

        // Infer TCompare from the left operand; right must be deserializable as the same type
        var leftElement = root[0];
        var compareType = leftElement.InferJsonExpressionType<TIn>();
        var expressionType = typeof(IJsonExpression<,>).MakeGenericType(typeof(TIn), compareType);

        var left = JsonSerializer.Deserialize(leftElement, expressionType, options) ??
            throw new JsonException(
                $"Expected: Valid expression for left operand of '!=', Actual: null after deserialization"
            );

        var right = JsonSerializer.Deserialize(root[1], expressionType, options) ??
            throw new JsonException(
                $"Expected: Valid expression for right operand of '!=', Actual: null after deserialization"
            );

        var notEqualsType = typeof(JsonExpressionSymbolNotEquals<,,>).MakeGenericType(typeof(TIn), compareType, typeof(TOut));
        var constructor = notEqualsType.GetConstructor([expressionType, expressionType]) ??
            throw new JsonException(
                $"Unable to find constructor for JsonExpressionSymbolNotEquals<{typeof(TIn).Name}, {compareType.Name}, {typeof(TOut).Name}>"
            );

        return (IJsonExpressionSymbolNotEquals<TIn, TOut>)constructor.Invoke([left, right]);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionSymbolNotEquals<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
