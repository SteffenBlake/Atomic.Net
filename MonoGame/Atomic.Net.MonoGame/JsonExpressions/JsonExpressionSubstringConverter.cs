using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionSubstring.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionSubstringConverter<TIn, TOut> : JsonConverter<IJsonExpressionSubstring<TIn, TOut>>
{
    public override IJsonExpressionSubstring<TIn, TOut>? Read(
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
                $"Expected: Array for substring operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength is < 2 or > 3)
        {
            throw new JsonException(
                $"Expected: Array with 2 or 3 elements for substring operator, Actual: Array with {arrayLength} elements"
            );
        }

        var @string = JsonSerializer.Deserialize<IJsonExpression<TIn, string>>(root[0], options);
        var start = JsonSerializer.Deserialize<IJsonExpression<TIn, int>>(root[1], options);

        if (@string is null || start is null)
        {
            throw new JsonException(
                $"Expected: Valid expressions for substring string and start, Actual: One or both are null"
            );
        }

        IJsonExpression<TIn, int>? length = null;
        if (arrayLength == 3)
        {
            length = JsonSerializer.Deserialize<IJsonExpression<TIn, int>>(root[2], options);
            if (length is null)
            {
                throw new JsonException(
                    $"Expected: Valid expression for substring length, Actual: null"
                );
            }
        }

        return new JsonExpressionSubstring<TIn, TOut>(@string, start, length);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionSubstring<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
