using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionStringLiteral.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Requested output type</typeparam>
public sealed class JsonExpressionStringLiteralConverter<TIn, TOut> : JsonConverter<IJsonExpressionStringLiteral<TIn, TOut>>
{
    public override IJsonExpressionStringLiteral<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // Validate that we're reading a string
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                $"Expected: String token for string literal, Actual: {reader.TokenType}"
            );
        }

        // Parse the string value
        var value = reader.GetString();

        return new JsonExpressionStringLiteral<TIn, TOut>(value);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionStringLiteral<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
