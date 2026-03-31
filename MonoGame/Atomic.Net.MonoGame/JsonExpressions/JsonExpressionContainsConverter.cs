using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionContains.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionContainsConverter<TIn, TOut> : JsonConverter<IJsonExpressionContains<TIn, TOut>>
{
    public override IJsonExpressionContains<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array for contains operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for contains operator, Actual: Array with {arrayLength} elements"
            );
        }

        // Infer element type from the collection operand (should be TElement[])
        var collectionType = root[0].InferJsonExpressionType<TIn>();
        var elementType = collectionType.IsArray
            ? collectionType.GetElementType()!
            : throw new JsonException(
                $"Expected: Array type for contains collection, Actual: {collectionType.Name}"
            );

        var collectionExprType = typeof(IJsonExpression<,>).MakeGenericType(typeof(TIn), collectionType);
        var itemExprType = typeof(IJsonExpression<,>).MakeGenericType(typeof(TIn), elementType);

        var collection = JsonSerializer.Deserialize(root[0], collectionExprType, options) ??
            throw new JsonException(
                $"Expected: Valid expression for contains collection, Actual: null after deserialization"
            );

        var item = JsonSerializer.Deserialize(root[1], itemExprType, options) ??
            throw new JsonException(
                $"Expected: Valid expression for contains item, Actual: null after deserialization"
            );

        var containsType = typeof(JsonExpressionContains<,,>).MakeGenericType(typeof(TIn), typeof(TOut), elementType);
        var constructor = containsType.GetConstructor([collectionExprType, itemExprType]) ??
            throw new JsonException(
                $"Unable to find constructor for JsonExpressionContains<{typeof(TIn).Name}, {typeof(TOut).Name}, {elementType.Name}>"
            );

        return (IJsonExpressionContains<TIn, TOut>)constructor.Invoke([collection, item]);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionContains<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
