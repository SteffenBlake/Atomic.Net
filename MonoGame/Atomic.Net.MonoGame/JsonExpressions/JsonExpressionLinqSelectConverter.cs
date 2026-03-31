using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionLinqSelect.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type (result array)</typeparam>
public sealed class JsonExpressionLinqSelectConverter<TIn, TOut> : JsonConverter<IJsonExpressionLinqSelect<TIn, TOut>>
{
    public override IJsonExpressionLinqSelect<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (!typeof(TOut).IsArray)
        {
            throw new JsonException(
                $"'select' operator requires TOut to be an array type, got TOut={typeof(TOut).Name}"
            );
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array for select operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for select operator, Actual: Array with {arrayLength} elements"
            );
        }

        // Infer source element type from the source array
        var sourceArrayType = root[0].InferJsonExpressionType<TIn>();
        var sourceElementType = sourceArrayType.IsArray
            ? sourceArrayType.GetElementType()!
            : throw new JsonException($"Expected array type for select source, Actual: {sourceArrayType.Name}");

        // TOut should be TResult[] — get the result element type
        var resultElementType = typeof(TOut).GetElementType()!;

        var sourceExprType = typeof(IJsonExpression<,>).MakeGenericType(typeof(TIn), sourceArrayType);
        var selectorExprType = typeof(IJsonExpression<,>).MakeGenericType(sourceElementType, resultElementType);

        var source = JsonSerializer.Deserialize(root[0], sourceExprType, options) ??
            throw new JsonException("Expected: Valid expression for select source, Actual: null after deserialization");

        var selector = JsonSerializer.Deserialize(root[1], selectorExprType, options) ??
            throw new JsonException("Expected: Valid expression for select selector, Actual: null after deserialization");

        var concreteType = typeof(JsonExpressionLinqSelect<,,>).MakeGenericType(typeof(TIn), resultElementType, sourceElementType);
        var ctor = concreteType.GetConstructor([sourceExprType, selectorExprType])!;
        return (IJsonExpressionLinqSelect<TIn, TOut>)ctor.Invoke([source, selector]);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionLinqSelect<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
