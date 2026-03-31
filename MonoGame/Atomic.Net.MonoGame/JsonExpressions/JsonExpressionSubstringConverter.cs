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
        if (typeof(TOut) != typeof(string))
        {
            throw new JsonException(
                $"'substring' operator requires TOut=string, got TOut={typeof(TOut).Name}"
            );
        }

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

        // Reject negative start literals at compile time (C# does not support negative indices)
        if (root[1].ValueKind == JsonValueKind.Number && root[1].GetSingle() < 0)
        {
            throw new JsonException("Substring start cannot be negative");
        }

        var start = JsonSerializer.Deserialize<IJsonExpression<TIn, float>>(root[1], options);

        if (@string is null || start is null)
        {
            throw new JsonException(
                $"Expected: Valid expressions for substring string and start, Actual: One or both are null"
            );
        }

        IJsonExpression<TIn, float>? length = null;
        if (arrayLength == 3)
        {
            // Reject negative length literals at compile time
            if (root[2].ValueKind == JsonValueKind.Number && root[2].GetSingle() < 0)
            {
                throw new JsonException("Substring length cannot be negative");
            }

            length = JsonSerializer.Deserialize<IJsonExpression<TIn, float>>(root[2], options);
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
