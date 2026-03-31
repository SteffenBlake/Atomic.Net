using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionStringContains.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionStringContainsConverter<TIn, TOut> : JsonConverter<IJsonExpressionStringContains<TIn, TOut>>
{
    public override IJsonExpressionStringContains<TIn, TOut>? Read(
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
                $"Expected: Array for stringContains operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for stringContains operator, Actual: Array with {arrayLength} elements"
            );
        }

        var haystack = JsonSerializer.Deserialize<IJsonExpression<TIn, string>>(root[0], options);
        var needle = JsonSerializer.Deserialize<IJsonExpression<TIn, string>>(root[1], options);

        if (haystack is null || needle is null)
        {
            throw new JsonException(
                $"Expected: Valid expressions for stringContains haystack and needle, Actual: One or both are null"
            );
        }

        return new JsonExpressionStringContains<TIn, TOut>(haystack, needle);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionStringContains<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
