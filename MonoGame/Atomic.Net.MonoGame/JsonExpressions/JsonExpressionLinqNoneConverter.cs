using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionLinqNone.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type (always bool)</typeparam>
public sealed class JsonExpressionLinqNoneConverter<TIn, TOut> : JsonConverter<IJsonExpressionLinqNone<TIn, TOut>>
{
    public override IJsonExpressionLinqNone<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (typeof(TOut) != typeof(bool))
        {
            throw new JsonException(
                $"'none' operator requires TOut=bool, got TOut={typeof(TOut).Name}"
            );
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array for none operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for none operator, Actual: Array with {arrayLength} elements"
            );
        }

        // Infer element type from the source array expression; empty array defaults to float[]
        var elementType = root[0].ValueKind == JsonValueKind.Array && root[0].GetArrayLength() == 0
            ? typeof(float)
            : root[0].InferJsonExpressionType<TIn>() is { IsArray: true } t
                ? t.GetElementType()!
                : throw new JsonException($"Expected array type for none source");

        var sourceArrayType = elementType.MakeArrayType();

        var sourceExprType = typeof(IJsonExpression<,>).MakeGenericType(typeof(TIn), sourceArrayType);
        var predicateExprType = typeof(IJsonExpression<,>).MakeGenericType(elementType, typeof(bool));

        var source = JsonSerializer.Deserialize(root[0], sourceExprType, options) ??
            throw new JsonException("Expected: Valid expression for none source, Actual: null after deserialization");

        var predicate = JsonSerializer.Deserialize(root[1], predicateExprType, options) ??
            throw new JsonException("Expected: Valid expression for none predicate, Actual: null after deserialization");

        var concreteType = typeof(JsonExpressionLinqNone<,,>).MakeGenericType(typeof(TIn), typeof(TOut), elementType);
        var ctor = concreteType.GetConstructor([sourceExprType, predicateExprType])!;
        return (IJsonExpressionLinqNone<TIn, TOut>)ctor.Invoke([source, predicate]);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionLinqNone<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
