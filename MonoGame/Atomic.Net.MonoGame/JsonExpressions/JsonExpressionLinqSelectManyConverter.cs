using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionLinqSelectMany.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type (flattened result array)</typeparam>
public sealed class JsonExpressionLinqSelectManyConverter<TIn, TOut> : JsonConverter<IJsonExpressionLinqSelectMany<TIn, TOut>>
{
    public override IJsonExpressionLinqSelectMany<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (!typeof(TOut).IsArray)
        {
            throw new JsonException(
                $"'selectMany' operator requires TOut to be an array type, got TOut={typeof(TOut).Name}"
            );
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array for selectMany operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for selectMany operator, Actual: Array with {arrayLength} elements"
            );
        }

        var sourceArrayType = root[0].InferJsonExpressionTypeInternal<TIn>();
        var sourceElementType = sourceArrayType.IsArray
            ? sourceArrayType.GetElementType()!
            : throw new JsonException($"Expected array type for selectMany source, Actual: {sourceArrayType.Name}");

        // TOut is TResult[] — get TResult
        var resultElementType = typeof(TOut).GetElementType()!;

        var resultArrayType = resultElementType.MakeArrayType();
        var sourceExprType = typeof(IJsonExpression<,>).MakeGenericType(typeof(TIn), sourceArrayType);
        var selectorExprType = typeof(IJsonExpression<,>).MakeGenericType(sourceElementType, resultArrayType);

        var source = JsonSerializer.Deserialize(root[0], sourceExprType, options) ??
            throw new JsonException("Expected: Valid expression for selectMany source, Actual: null after deserialization");

        var selector = JsonSerializer.Deserialize(root[1], selectorExprType, options) ??
            throw new JsonException("Expected: Valid expression for selectMany selector, Actual: null after deserialization");

        var concreteType = typeof(JsonExpressionLinqSelectMany<,,>).MakeGenericType(typeof(TIn), resultElementType, sourceElementType);
        var ctor = concreteType.GetConstructor([sourceExprType, selectorExprType])!;
        return (IJsonExpressionLinqSelectMany<TIn, TOut>)ctor.Invoke([source, selector]);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionLinqSelectMany<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
