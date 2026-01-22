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
            _ => default
        };
    }

    public override void Write(Utf8JsonWriter writer, PropertyValue value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

