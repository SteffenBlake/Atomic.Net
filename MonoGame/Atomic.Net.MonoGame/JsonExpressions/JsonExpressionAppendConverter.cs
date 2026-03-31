using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionAppend.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionAppendConverter<TIn, TOut> : JsonConverter<JsonExpressionAppend<TIn, TOut>>
{
    public override JsonExpressionAppend<TIn, TOut>? Read(
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

        var first = JsonSerializer.Deserialize<JsonExpression<TIn, TOut>>(root[0], options);
        var second = JsonSerializer.Deserialize<JsonExpression<TIn, TOut>>(root[1], options);

        if (first is null || second is null)
        {
            throw new JsonException(
                $"Expected: Valid expressions for append first and second, Actual: One or both are null"
            );
        }

        return new JsonExpressionAppend<TIn, TOut>(first, second);
    }

    public override void Write(
        Utf8JsonWriter writer,
        JsonExpressionAppend<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
