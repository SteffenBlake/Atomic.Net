using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionNumberLiteral.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Requested output type</typeparam>
public sealed class JsonExpressionNumberLiteralConverter<TIn, TOut> : JsonConverter<IJsonExpressionNumberLiteral<TIn, TOut>>
{
    public override IJsonExpressionNumberLiteral<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (typeof(TOut).IsArray)
        {
            throw new JsonException(
                $"Number literal cannot produce array type TOut={typeof(TOut).Name}"
            );
        }

        // Validate that we're reading a number
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException(
                $"Expected: Number token for number literal, Actual: {reader.TokenType}"
            );
        }

        // Parse the number value as float
        if (!reader.TryGetSingle(out var value))
        {
            throw new JsonException(
                $"Expected: Valid float number, Actual: Could not parse '{reader.GetString()}' as float"
            );
        }

        return new JsonExpressionNumberLiteral<TIn, TOut>(value);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionNumberLiteral<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
