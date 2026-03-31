using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionContains.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionContainsConverter<TIn, TOut> : JsonConverter<JsonExpressionContains<TIn, TOut>>
{
    public override JsonExpressionContains<TIn, TOut>? Read(
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
                $"Expected: Array for contains operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for contains operator, Actual: Array with {arrayLength} elements"
            );
        }

        var collection = JsonSerializer.Deserialize<JsonExpression<TIn, TOut>>(root[0], options);
        var item = JsonSerializer.Deserialize<JsonExpression<TIn, TOut>>(root[1], options);

        if (collection is null || item is null)
        {
            throw new JsonException(
                $"Expected: Valid expressions for contains collection and item, Actual: One or both are null"
            );
        }

        return new JsonExpressionContains<TIn, TOut>(collection, item);
    }

    public override void Write(
        Utf8JsonWriter writer,
        JsonExpressionContains<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
