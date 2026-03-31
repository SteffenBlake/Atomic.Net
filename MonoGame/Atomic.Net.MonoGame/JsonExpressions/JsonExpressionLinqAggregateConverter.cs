using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionLinqAggregate.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Accumulator / output type</typeparam>
public sealed class JsonExpressionLinqAggregateConverter<TIn, TOut> : JsonConverter<IJsonExpressionLinqAggregate<TIn, TOut>>
{
    public override IJsonExpressionLinqAggregate<TIn, TOut>? Read(
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
                $"Expected: Array for aggregate operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength != 3)
        {
            throw new JsonException(
                $"Expected: Array with 3 elements for aggregate operator, Actual: Array with {arrayLength} elements"
            );
        }

        var sourceArrayType = root[0].InferJsonExpressionType<TIn>();
        var sourceElementType = sourceArrayType.IsArray
            ? sourceArrayType.GetElementType()!
            : throw new JsonException($"Expected array type for aggregate source, Actual: {sourceArrayType.Name}");

        var sourceExprType = typeof(IJsonExpression<,>).MakeGenericType(typeof(TIn), sourceArrayType);
        var contextType = typeof(JsonAggregateContext<,>).MakeGenericType(sourceElementType, typeof(TOut));
        var accumulatorExprType = typeof(IJsonExpression<,>).MakeGenericType(contextType, typeof(TOut));
        var initialValueExprType = typeof(IJsonExpression<,>).MakeGenericType(typeof(TIn), typeof(TOut));

        var source = JsonSerializer.Deserialize(root[0], sourceExprType, options) ??
            throw new JsonException("Expected: Valid expression for aggregate source, Actual: null after deserialization");

        var accumulator = JsonSerializer.Deserialize(root[1], accumulatorExprType, options) ??
            throw new JsonException("Expected: Valid expression for aggregate accumulator, Actual: null after deserialization");

        var initialValue = JsonSerializer.Deserialize(root[2], initialValueExprType, options) ??
            throw new JsonException("Expected: Valid expression for aggregate initialValue, Actual: null after deserialization");

        var concreteType = typeof(JsonExpressionLinqAggregate<,,>).MakeGenericType(typeof(TIn), typeof(TOut), sourceElementType);
        var ctor = concreteType.GetConstructor([sourceExprType, accumulatorExprType, initialValueExprType])!;
        return (IJsonExpressionLinqAggregate<TIn, TOut>)ctor.Invoke([source, accumulator, initialValue]);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionLinqAggregate<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
