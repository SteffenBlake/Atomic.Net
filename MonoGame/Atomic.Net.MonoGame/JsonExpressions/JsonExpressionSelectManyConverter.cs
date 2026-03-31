using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionSelectMany.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TSource">Source array element type</typeparam>
/// <typeparam name="TResult">Result element type after flattening</typeparam>
public sealed class JsonExpressionSelectManyConverter<TIn, TSource, TResult> : JsonConverter<JsonExpressionSelectMany<TIn, TSource, TResult>>
{
    public override JsonExpressionSelectMany<TIn, TSource, TResult>? Read(
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

        var source = JsonSerializer.Deserialize<JsonExpression<TIn, TSource[]>>(root[0], options);
        var selector = JsonSerializer.Deserialize<JsonExpression<TSource, TResult[]>>(root[1], options);

        if (source is null || selector is null)
        {
            throw new JsonException(
                $"Expected: Valid expressions for selectMany source and selector, Actual: One or both are null"
            );
        }

        return new JsonExpressionSelectMany<TIn, TSource, TResult>(source, selector);
    }

    public override void Write(
        Utf8JsonWriter writer,
        JsonExpressionSelectMany<TIn, TSource, TResult> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
