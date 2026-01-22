using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.BED.Properties;

public class PropertiesBehaviorConverter : JsonConverter<PropertiesBehavior>
{
    public override PropertiesBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.StartObject => ReadDictionary(ref reader, options),
            _ => default
        };
    }

    private static PropertiesBehavior ReadDictionary(
        ref Utf8JsonReader reader, JsonSerializerOptions options
    )
    {
        // Try parse a dictionary, fallback to an empty dict
        var value = JsonSerializer
            .Deserialize<Dictionary<string, PropertyValue>>(ref reader, options)
            ?? new Dictionary<string, PropertyValue>(StringComparer.OrdinalIgnoreCase);

        return new(value);
    }

    public override void Write(Utf8JsonWriter writer, PropertiesBehavior value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

