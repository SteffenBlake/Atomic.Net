using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionContains.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionInConverter<TIn, TOut> : JsonConverter<IJsonExpressionIn<TIn, TOut>>
{
    public override IJsonExpressionIn<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (typeof(TOut) != typeof(bool))
        {
            throw new JsonException(
                $"'in' operator requires TOut=bool, got TOut={typeof(TOut).Name}"
            );
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array for 'in' operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for 'in' operator, Actual: Array with {arrayLength} elements"
            );
        }

        // Infer element type: from the collection (index 1) if non-empty, otherwise from the needle (index 0)
        Type elementType;
        if (root[1].ValueKind == JsonValueKind.Array && root[1].GetArrayLength() == 0)
        {
            // Empty array literal — infer element type from the needle
            elementType = root[0].InferJsonExpressionType<TIn>();
        }
        else
        {
            var collectionType = root[1].InferJsonExpressionType<TIn>();
            elementType = collectionType.IsArray
                ? collectionType.GetElementType()!
                : throw new JsonException(
                    $"Expected: Array type for 'in' collection, Actual: {collectionType.Name}"
                );
        }

        var collectionType2 = elementType.MakeArrayType();
        var collectionExprType = typeof(IJsonExpression<,>).MakeGenericType(typeof(TIn), collectionType2);
        var itemExprType = typeof(IJsonExpression<,>).MakeGenericType(typeof(TIn), elementType);

        var collection = JsonSerializer.Deserialize(root[1], collectionExprType, options) ??
            throw new JsonException(
                $"Expected: Valid expression for 'in' collection, Actual: null after deserialization"
            );

        var item = JsonSerializer.Deserialize(root[0], itemExprType, options) ??
            throw new JsonException(
                $"Expected: Valid expression for 'in' item, Actual: null after deserialization"
            );

        var inType = typeof(JsonExpressionIn<,,>).MakeGenericType(typeof(TIn), typeof(TOut), elementType);
        var constructor = inType.GetConstructor([collectionExprType, itemExprType]) ??
            throw new JsonException(
                $"Unable to find constructor for JsonExpressionIn<{typeof(TIn).Name}, {typeof(TOut).Name}, {elementType.Name}>"
            );

        return (IJsonExpressionIn<TIn, TOut>)constructor.Invoke([collection, item]);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionIn<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
