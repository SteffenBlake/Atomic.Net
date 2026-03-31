using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionLinqSelect.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TSource">Source array element type</typeparam>
/// <typeparam name="TResult">Result element type after transformation</typeparam>
public sealed class JsonExpressionLinqSelectConverter<TIn, TSource, TResult> : JsonConverter<IJsonExpressionLinqSelect<TIn, TResult[]>>
{
    public override IJsonExpressionLinqSelect<TIn, TResult[]>? Read(
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

        var source = JsonSerializer.Deserialize<IJsonExpression<TIn, TSource[]>>(root[0], options) ??
            throw new JsonException(
                $"Expected: Valid expression for select source, Actual: null after deserialization"
            );

        var selector = JsonSerializer.Deserialize<IJsonExpression<TSource, TResult>>(root[1], options) ??
            throw new JsonException(
                $"Expected: Valid expression for select selector, Actual: null after deserialization"
            );

        return new JsonExpressionLinqSelect<TIn, TResult, TSource>(source, selector);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionLinqSelect<TIn, TResult[]> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
