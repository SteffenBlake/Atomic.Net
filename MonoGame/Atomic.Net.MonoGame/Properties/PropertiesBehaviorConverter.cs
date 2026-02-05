using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Properties;

/// <summary>
/// JSON converter for PropertiesBehavior.
/// Validates property keys and throws JsonException for invalid entries.
/// </summary>
public class PropertiesBehaviorConverter : JsonConverter<PropertiesBehavior>
{
    public override PropertiesBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Guard: null value
        if (reader.TokenType == JsonTokenType.Null)
        {
            throw new JsonException("Properties object cannot be null");
        }

        // Guard: not an object
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Properties must be an object, found {reader.TokenType}");
        }

        FluentDictionary<string, PropertyValue> result = new(8, StringComparer.InvariantCultureIgnoreCase);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var key = reader.GetString();

            // Guard: empty or whitespace key
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new JsonException("Property key cannot be empty or whitespace");
            }

            // Guard: duplicate keys (case-insensitive)
            if (result.ContainsKey(key))
            {
                throw new JsonException($"Duplicate property key '{key}' detected (keys are case-insensitive)");
            }

            // Read the value
            reader.Read();
            var value = JsonSerializer.Deserialize<PropertyValue>(ref reader, options);

            // Guard: null values are skipped (not added to dictionary)
            // PropertyValue default is "empty" type, which means null was encountered
            if (value.Equals(default))
            {
                continue;
            }

            result.Add(key, value);
        }

        return new PropertiesBehavior
        {
            Properties = result
        };
    }

    public override void Write(Utf8JsonWriter writer, PropertiesBehavior value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Properties, options);
    }
}

