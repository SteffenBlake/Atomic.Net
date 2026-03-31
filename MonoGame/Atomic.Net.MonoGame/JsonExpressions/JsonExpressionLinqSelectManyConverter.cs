using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionLinqSelectMany.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TSource">Source array element type</typeparam>
/// <typeparam name="TResult">Result element type after flattening</typeparam>
public sealed class JsonExpressionLinqSelectManyConverter<TIn, TSource, TResult> : JsonConverter<IJsonExpressionLinqSelectMany<TIn, TResult[]>>
{
    public override IJsonExpressionLinqSelectMany<TIn, TResult[]>? Read(
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
                $"Expected: Array for selectMany operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for selectMany operator, Actual: Array with {arrayLength} elements"
            );
        }

        var source = JsonSerializer.Deserialize<IJsonExpression<TIn, TSource[]>>(root[0], options) ??
            throw new JsonException(
                $"Expected: Valid expression for selectMany source, Actual: null after deserialization"
            );

        var selector = JsonSerializer.Deserialize<IJsonExpression<TSource, TResult[]>>(root[1], options) ??
            throw new JsonException(
                $"Expected: Valid expression for selectMany selector, Actual: null after deserialization"
            );

        return new JsonExpressionLinqSelectMany<TIn, TResult, TSource>(source, selector);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionLinqSelectMany<TIn, TResult[]> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
