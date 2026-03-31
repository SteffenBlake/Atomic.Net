using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionLog.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionLogConverter<TIn, TOut> : JsonConverter<IJsonExpressionLog<TIn, TOut>>
{
    public override IJsonExpressionLog<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Log can be direct value or 1-element array
        IJsonExpression<TIn, TOut>? value;

        if (root.ValueKind == JsonValueKind.Array)
        {
            var arrayLength = root.GetArrayLength();
            if (arrayLength != 1)
            {
                throw new JsonException(
                    $"Expected: Array with 1 element for log operator, Actual: Array with {arrayLength} elements"
                );
            }

            value = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(root[0], options);
        }
        else
        {
            // Direct value (could be literal, object, etc.)
            value = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(root, options);
        }

        if (value is null)
        {
            throw new JsonException(
                $"Expected: Valid expression for log value, Actual: null"
            );
        }

        return new JsonExpressionLog<TIn, TOut>(value);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionLog<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
