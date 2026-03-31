using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionLinqAll.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type (always bool)</typeparam>
public sealed class JsonExpressionLinqAllConverter<TIn, TOut> : JsonConverter<IJsonExpressionLinqAll<TIn, TOut>>
{
    public override IJsonExpressionLinqAll<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (typeof(TOut) != typeof(bool))
        {
            throw new JsonException(
                $"'all' operator requires TOut=bool, got TOut={typeof(TOut).Name}"
            );
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array for all operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for all operator, Actual: Array with {arrayLength} elements"
            );
        }

        // Infer element type from the source array expression; empty array defaults to float[]
        var elementType = root[0].ValueKind == JsonValueKind.Array && root[0].GetArrayLength() == 0
            ? typeof(float)
            : root[0].InferJsonExpressionType<TIn>() is { IsArray: true } t
                ? t.GetElementType()!
                : throw new JsonException($"Expected array type for all source");

        var sourceArrayType = elementType.MakeArrayType();

        var sourceExprType = typeof(IJsonExpression<,>).MakeGenericType(typeof(TIn), sourceArrayType);
        var predicateExprType = typeof(IJsonExpression<,>).MakeGenericType(elementType, typeof(bool));

        var source = JsonSerializer.Deserialize(root[0], sourceExprType, options) ??
            throw new JsonException("Expected: Valid expression for all source, Actual: null after deserialization");

        var predicate = JsonSerializer.Deserialize(root[1], predicateExprType, options) ??
            throw new JsonException("Expected: Valid expression for all predicate, Actual: null after deserialization");

        var concreteType = typeof(JsonExpressionLinqAll<,,>).MakeGenericType(typeof(TIn), typeof(TOut), elementType);
        var ctor = concreteType.GetConstructor([sourceExprType, predicateExprType])!;
        return (IJsonExpressionLinqAll<TIn, TOut>)ctor.Invoke([source, predicate]);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionLinqAll<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
