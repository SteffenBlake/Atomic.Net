using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Custom JSON converter for SceneCommand that handles discriminator-based deserialization.
/// Supports 'mut' command type and validates the command structure.
/// </summary>
public class SceneCommandConverter : JsonConverter<SceneCommand>
{
    public override SceneCommand Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonNode = JsonNode.Parse(ref reader);

        if (jsonNode is not JsonObject jsonObject)
        {
            throw new JsonException("Expected JSON object");
        }

        var firstProperty = jsonObject.First();
        var propertyKey = firstProperty.Key.ToLower();

        return propertyKey switch
        {
            "mut" => firstProperty.Value is not null
                ? new MutCommand(firstProperty.Value.Deserialize<MutOperation[]>(options) 
                    ?? throw new JsonException("Failed to deserialize 'mut' array"))
                : throw new JsonException("'mut' command value cannot be null"),
            _ => throw new JsonException($"Unrecognized object discriminator key: '{propertyKey}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, SceneCommand value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

