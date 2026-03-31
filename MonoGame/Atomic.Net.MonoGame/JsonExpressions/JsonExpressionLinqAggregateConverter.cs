using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionLinqAggregate.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TSource">Source array element type</typeparam>
/// <typeparam name="TAccumulate">Accumulator type</typeparam>
public sealed class JsonExpressionLinqAggregateConverter<TIn, TSource, TAccumulate> : JsonConverter<IJsonExpressionLinqAggregate<TIn, TAccumulate>>
{
    public override IJsonExpressionLinqAggregate<TIn, TAccumulate>? Read(
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

        var source = JsonSerializer.Deserialize<IJsonExpression<TIn, TSource[]>>(root[0], options) ??
            throw new JsonException(
                $"Expected: Valid expression for aggregate source, Actual: null after deserialization"
            );

        var accumulator = JsonSerializer.Deserialize<IJsonExpression<TSource, TAccumulate>>(root[1], options) ??
            throw new JsonException(
                $"Expected: Valid expression for aggregate accumulator, Actual: null after deserialization"
            );

        var initialValue = JsonSerializer.Deserialize<IJsonExpression<TIn, TAccumulate>>(root[2], options) ??
            throw new JsonException(
                $"Expected: Valid expression for aggregate initialValue, Actual: null after deserialization"
            );

        return new JsonExpressionLinqAggregate<TIn, TAccumulate, TSource>(source, accumulator, initialValue);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionLinqAggregate<TIn, TAccumulate> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
