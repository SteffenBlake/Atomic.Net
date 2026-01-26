using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Properties;

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
        var dict = new Dictionary<string, PropertyValue>(StringComparer.OrdinalIgnoreCase);

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

            // Validate key
            if (string.IsNullOrWhiteSpace(key))
            {
                // test-architect: Per requirements, empty/whitespace keys fire ErrorEvent and throw
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Property key cannot be empty or whitespace"
                ));
                return default;
            }

            // Check for duplicate keys (case-insensitive)
            if (dict.ContainsKey(key))
            {
                // test-architect: Per requirements, duplicate keys fire ErrorEvent and throw
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Duplicate property key '{key}' detected (keys are case-insensitive)"
                ));

                return default;
            }

            // Read the value
            reader.Read();
            var value = JsonSerializer.Deserialize<PropertyValue>(ref reader, options);

            // test-architect: Per requirements, null values are skipped (not added to dictionary)
            // PropertyValue default is "empty" type, which means null was encountered
            if (value.Equals(default))
            {
                continue;
            }

            dict[key] = value;
        }

        return new(dict);
    }

    public override void Write(Utf8JsonWriter writer, PropertiesBehavior value, JsonSerializerOptions options)
    {
        if (value.Properties == null)
        {
            return;
        }
        JsonSerializer.Serialize(writer, value.Properties, options);
    }
}

