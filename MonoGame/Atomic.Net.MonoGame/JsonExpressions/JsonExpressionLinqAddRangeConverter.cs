using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionLinqAddRange.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionLinqAddRangeConverter<TIn, TOut> : JsonConverter<IJsonExpressionLinqAddRange<TIn, TOut>>
{
    public override IJsonExpressionLinqAddRange<TIn, TOut>? Read(
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
                $"Expected: Array for append operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for append operator, Actual: Array with {arrayLength} elements"
            );
        }

        var first = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(root[0], options);
        var second = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(root[1], options);

        if (first is null || second is null)
        {
            throw new JsonException(
                $"Expected: Valid expressions for append first and second, Actual: One or both are null"
            );
        }

        return new JsonExpressionLinqAddRange<TIn, TOut>(first, second);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionLinqAddRange<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
