using System;
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
        // TODO: Implement JSON parsing logic
        throw new NotImplementedException();
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
