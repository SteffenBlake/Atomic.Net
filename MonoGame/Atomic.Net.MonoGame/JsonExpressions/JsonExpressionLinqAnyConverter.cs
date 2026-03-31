using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionLinqAny.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TSource">Array element type</typeparam>
public sealed class JsonExpressionLinqAnyConverter<TIn, TSource> : JsonConverter<IJsonExpressionLinqAny<TIn, bool>>
{
    public override IJsonExpressionLinqAny<TIn, bool>? Read(
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

        var source = JsonSerializer.Deserialize<IJsonExpression<TIn, TSource[]>>(root[0], options) ??
            throw new JsonException(
                $"Expected: Valid expression for any source, Actual: null after deserialization"
            );

        var predicate = JsonSerializer.Deserialize<IJsonExpression<TSource, bool>>(root[1], options) ??
            throw new JsonException(
                $"Expected: Valid expression for any predicate, Actual: null after deserialization"
            );

        return new JsonExpressionLinqAny<TIn, bool, TSource>(source, predicate);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionLinqAny<TIn, bool> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
