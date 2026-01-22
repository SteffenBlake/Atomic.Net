using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.BED.Properties;

public class PropertyValueConverter : JsonConverter<PropertyValue>
{
    public override PropertyValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetSingle(),
            JsonTokenType.False => false,
            JsonTokenType.True => true,
            JsonTokenType.String => reader.GetString() ?? default(PropertyValue),
            JsonTokenType.StartArray => SkipComplexType(ref reader),
            JsonTokenType.StartObject => SkipComplexType(ref reader),
            _ => default
        };
    }

    private static PropertyValue SkipComplexType(ref Utf8JsonReader reader)
    {
        // test-architect: FINDING: When deserializing standalone arrays/objects, we must consume
        // all tokens to avoid "read too much or not enough" error. Using JsonDocument to consume.
        using var doc = JsonDocument.ParseValue(ref reader);
        return default;
    }

    public override void Write(
        Utf8JsonWriter writer, PropertyValue value, JsonSerializerOptions options
    )
    {
        value.Visit(
            s => JsonSerializer.Serialize(writer, s, options),
            f => JsonSerializer.Serialize(writer, f, options),
            b => JsonSerializer.Serialize(writer, b, options),
            writer.WriteNullValue
        );
    }
}

