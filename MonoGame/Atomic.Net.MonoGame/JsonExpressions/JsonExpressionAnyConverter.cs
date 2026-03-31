using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionAny.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TSource">Array element type</typeparam>
public sealed class JsonExpressionAnyConverter<TIn, TSource> : JsonConverter<JsonExpressionAny<TIn, TSource>>
{
    public override JsonExpressionAny<TIn, TSource>? Read(
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
                $"Expected: Array for any operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for any operator, Actual: Array with {arrayLength} elements"
            );
        }

        var source = JsonSerializer.Deserialize<JsonExpression<TIn, TSource[]>>(root[0], options);
        var predicate = JsonSerializer.Deserialize<JsonExpression<TSource, bool>>(root[1], options);

        if (source is null || predicate is null)
        {
            throw new JsonException(
                $"Expected: Valid expressions for any source and predicate, Actual: One or both are null"
            );
        }

        return new JsonExpressionAny<TIn, TSource>(source, predicate);
    }

    public override void Write(
        Utf8JsonWriter writer,
        JsonExpressionAny<TIn, TSource> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
