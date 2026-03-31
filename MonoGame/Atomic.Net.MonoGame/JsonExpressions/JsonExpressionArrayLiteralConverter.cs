using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionArrayLiteral.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Requested output type</typeparam>
public sealed class JsonExpressionArrayLiteralConverter<TIn, TOut> : JsonConverter<IJsonExpressionArrayLiteral<TIn, TOut>>
{
    public override IJsonExpressionArrayLiteral<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // Use the built-in fast path from Utf8JsonReader to JsonDocument
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Validate: must be an array
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array for array literal, Actual: {root.ValueKind}"
            );
        }

        // TOut is the array type — derive TElement from it
        var elementType = typeof(TOut).GetElementType() ??
            throw new JsonException($"Expected array type for array literal TOut, Actual: {typeof(TOut).Name}");

        var elementExprType = typeof(IJsonExpression<,>).MakeGenericType(typeof(TIn), elementType);
        var elementExprArrayType = elementExprType.MakeArrayType();

        // Deserialize all array elements as IJsonExpression<TIn, TElement>
        var arrayLength = root.GetArrayLength();
        var elements = (Array)Activator.CreateInstance(elementExprArrayType, arrayLength)!;

        for (int i = 0; i < arrayLength; i++)
        {
            var element = JsonSerializer.Deserialize(root[i], elementExprType, options) ??
                throw new JsonException(
                    $"Expected: Valid expression for element {i} of array literal, Actual: null after deserialization"
                );
            elements.SetValue(element, i);
        }

        var concreteType = typeof(JsonExpressionArrayLiteral<,,>).MakeGenericType(typeof(TIn), typeof(TOut), elementType);
        var ctor = concreteType.GetConstructor([elementExprArrayType])!;
        return (IJsonExpressionArrayLiteral<TIn, TOut>)ctor.Invoke([elements]);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionArrayLiteral<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
