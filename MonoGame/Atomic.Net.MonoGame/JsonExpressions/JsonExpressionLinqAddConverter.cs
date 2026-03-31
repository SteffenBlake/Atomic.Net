using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionLinqAdd.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionLinqAddConverter<TIn, TOut> : JsonConverter<JsonExpressionLinqAdd<TIn, TOut>>
{
    public override JsonExpressionLinqAdd<TIn, TOut>? Read(
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
                $"Expected: Array for linqAdd operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for linqAdd operator, Actual: Array with {arrayLength} elements"
            );
        }

        var item = JsonSerializer.Deserialize<JsonExpression<TIn, TOut>>(root[0], options);
        var array = JsonSerializer.Deserialize<JsonExpression<TIn, TOut>>(root[1], options);

        if (item is null || array is null)
        {
            throw new JsonException(
                $"Expected: Valid expressions for linqAdd item and array, Actual: One or both are null"
            );
        }

        return new JsonExpressionLinqAdd<TIn, TOut>(item, array);
    }

    public override void Write(
        Utf8JsonWriter writer,
        JsonExpressionLinqAdd<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
