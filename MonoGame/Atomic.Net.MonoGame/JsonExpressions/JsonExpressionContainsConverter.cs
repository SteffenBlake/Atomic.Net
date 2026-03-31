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

        var substring = JsonSerializer.Deserialize<IJsonExpression<TIn, string>>(root[0], options);
        var text = JsonSerializer.Deserialize<IJsonExpression<TIn, string>>(root[1], options);

        if (text is null || substring is null)
        {
            throw new JsonException(
                $"Expected: Valid expressions for 'contains' text and substring, Actual: One or both are null"
            );
        }

        return new JsonExpressionContains<TIn, TOut>(text, substring);
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
