using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionStringContains.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionContainsConverter<TIn, TOut> : JsonConverter<IJsonExpressionContains<TIn, TOut>>
{
    public override IJsonExpressionContains<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (typeof(TOut) != typeof(bool))
        {
            throw new JsonException(
                $"'contains' operator requires TOut=bool, got TOut={typeof(TOut).Name}"
            );
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array for 'contains' operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for 'contains' operator, Actual: Array with {arrayLength} elements"
            );
        }

        var needle = JsonSerializer.Deserialize<IJsonExpression<TIn, string>>(root[0], options);
        var haystack = JsonSerializer.Deserialize<IJsonExpression<TIn, string>>(root[1], options);

        if (haystack is null || needle is null)
        {
            throw new JsonException(
                $"Expected: Valid expressions for 'contains' haystack and needle, Actual: One or both are null"
            );
        }

        return new JsonExpressionContains<TIn, TOut>(haystack, needle);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionContains<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
