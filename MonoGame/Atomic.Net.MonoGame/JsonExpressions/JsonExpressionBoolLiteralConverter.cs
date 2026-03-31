using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionBoolLiteral.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Requested output type</typeparam>
public sealed class JsonExpressionBoolLiteralConverter<TIn, TOut> : JsonConverter<JsonExpressionBoolLiteral<TIn, TOut>>
{
    public override JsonExpressionBoolLiteral<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // Validate that we're reading a boolean
        if (reader.TokenType != JsonTokenType.True && reader.TokenType != JsonTokenType.False)
        {
            throw new JsonException(
                $"Expected: Boolean token for bool literal, Actual: {reader.TokenType}"
            );
        }

        // Parse the boolean value
        var value = reader.GetBoolean();

        return new JsonExpressionBoolLiteral<TIn, TOut>(value);
    }

    public override void Write(
        Utf8JsonWriter writer,
        JsonExpressionBoolLiteral<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
