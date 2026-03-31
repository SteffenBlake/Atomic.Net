using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionLinqWhere.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TSource">Array element type</typeparam>
public sealed class JsonExpressionLinqWhereConverter<TIn, TSource> : JsonConverter<IJsonExpressionLinqWhere<TIn, TSource[]>>
{
    public override IJsonExpressionLinqWhere<TIn, TSource[]>? Read(
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

        var source = JsonSerializer.Deserialize<IJsonExpression<TIn, TSource[]>>(root[0], options) ??
            throw new JsonException(
                $"Expected: Valid expression for where source, Actual: null after deserialization"
            );

        var predicate = JsonSerializer.Deserialize<IJsonExpression<TSource, bool>>(root[1], options) ??
            throw new JsonException(
                $"Expected: Valid expression for where predicate, Actual: null after deserialization"
            );

        return new JsonExpressionLinqWhere<TIn, TSource[], TSource>(source, predicate);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionLinqWhere<TIn, TSource[]> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
