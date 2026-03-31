using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionAggregate.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TSource">Source array element type</typeparam>
/// <typeparam name="TAccumulate">Accumulator type</typeparam>
public sealed class JsonExpressionAggregateConverter<TIn, TSource, TAccumulate> : JsonConverter<JsonExpressionAggregate<TIn, TSource, TAccumulate>>
{
    public override JsonExpressionAggregate<TIn, TSource, TAccumulate>? Read(
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

        var source = JsonSerializer.Deserialize<JsonExpression<TIn, TSource[]>>(root[0], options);
        var accumulator = JsonSerializer.Deserialize<JsonExpression<TSource, TAccumulate>>(root[1], options);
        var initialValue = JsonSerializer.Deserialize<JsonExpression<TIn, TAccumulate>>(root[2], options);

        if (source is null || accumulator is null || initialValue is null)
        {
            throw new JsonException(
                $"Expected: Valid expressions for aggregate source, accumulator, and initialValue, Actual: One or more are null"
            );
        }

        return new JsonExpressionAggregate<TIn, TSource, TAccumulate>(source, accumulator, initialValue);
    }

    public override void Write(
        Utf8JsonWriter writer,
        JsonExpressionAggregate<TIn, TSource, TAccumulate> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
