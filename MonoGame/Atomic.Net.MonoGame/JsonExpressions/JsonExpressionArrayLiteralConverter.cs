using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionArrayLiteral.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Requested output type</typeparam>
public sealed class JsonExpressionArrayLiteralConverter<TIn, TOut> : JsonConverter<IJsonExpressionArrayLiteral<TIn, TOut>>
{
    public override IJsonExpressionArrayLiteral<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // Use the built-in fast path from Utf8JsonReader to JsonDocument
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Validate: must be an array
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array for array literal, Actual: {root.ValueKind}"
            );
        }

        // Deserialize all array elements
        var arrayLength = root.GetArrayLength();
        var elements = new IJsonExpression<TIn, TOut>[arrayLength];
        
        for (int i = 0; i < arrayLength; i++)
        {
            var element = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(root[i], options);
            if (element is null)
            {
                throw new JsonException(
                    $"Expected: Valid expression for element {i} of array literal, Actual: null after deserialization"
                );
            }
            elements[i] = element;
        }

        return new JsonExpressionArrayLiteral<TIn, TOut>(elements);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionArrayLiteral<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
