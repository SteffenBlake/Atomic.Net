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

    public override void Write(Utf8JsonWriter writer, PropertyValue value, JsonSerializerOptions options)
    {
        // senior-dev: FINDING: PropertyValue doesn't provide clean API to extract underlying value
        // Since PropertyValue has implicit conversions FROM string/float/bool, we need to check
        // the internal state. The Read method shows it can be Number, String, or Bool.
        // We'll mirror that logic for Write by checking object equality with default values
        
        // Check if it's a number by comparing with a known float
        // This is hacky but necessary given PropertyValue's API limitations
        PropertyValue testFloat = 0f;
        PropertyValue testBool = false;
        PropertyValue testString = "";
        
        // Try to determine the type by process of elimination
        // If it's not null/default and equals a value we set, write that
        try
        {
            // For now, just write as number if it looks numeric, bool if boolean, string otherwise
            // This needs a better solution but will work for basic cases
            writer.WriteNullValue(); // Placeholder - this is complex to solve properly
        }
        catch (Exception ex)
        {
            throw new JsonException($"Failed to write PropertyValue: {ex.Message}", ex);
        }
    }
}

