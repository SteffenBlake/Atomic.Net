using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionLinqWhere.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type (filtered array)</typeparam>
public sealed class JsonExpressionLinqWhereConverter<TIn, TOut> : JsonConverter<IJsonExpressionLinqWhere<TIn, TOut>>
{
    public override IJsonExpressionLinqWhere<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (!typeof(TOut).IsArray)
        {
            throw new JsonException(
                $"'where' operator requires TOut to be an array type, got TOut={typeof(TOut).Name}"
            );
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array for where operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for where operator, Actual: Array with {arrayLength} elements"
            );
        }

        var sourceArrayType = root[0].InferJsonExpressionType<TIn>();
        var elementType = sourceArrayType.IsArray
            ? sourceArrayType.GetElementType()!
            : throw new JsonException($"Expected array type for where source, Actual: {sourceArrayType.Name}");

        var sourceExprType = typeof(IJsonExpression<,>).MakeGenericType(typeof(TIn), sourceArrayType);
        var predicateExprType = typeof(IJsonExpression<,>).MakeGenericType(elementType, typeof(bool));

        var source = JsonSerializer.Deserialize(root[0], sourceExprType, options) ??
            throw new JsonException("Expected: Valid expression for where source, Actual: null after deserialization");

        var predicate = JsonSerializer.Deserialize(root[1], predicateExprType, options) ??
            throw new JsonException("Expected: Valid expression for where predicate, Actual: null after deserialization");

        var concreteType = typeof(JsonExpressionLinqWhere<,,>).MakeGenericType(typeof(TIn), typeof(TOut), elementType);
        var ctor = concreteType.GetConstructor([sourceExprType, predicateExprType])!;
        return (IJsonExpressionLinqWhere<TIn, TOut>)ctor.Invoke([source, predicate]);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionLinqWhere<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
