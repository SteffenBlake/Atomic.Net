using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Sequencing;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Custom JSON converter for SceneCommand that handles discriminator-based deserialization.
/// Supports 'mut', 'sequence-start', 'sequence-stop', 'sequence-reset' command types.
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
            
            "sequence-start" => firstProperty.Value is not null
                ? new SequenceStartCommand(firstProperty.Value.Deserialize<string>(options) 
                    ?? throw new JsonException("Failed to deserialize 'sequence-start' value"))
                : throw new JsonException("'sequence-start' command value cannot be null"),
            
            "sequence-stop" => firstProperty.Value is not null
                ? new SequenceStopCommand(firstProperty.Value.Deserialize<string>(options) 
                    ?? throw new JsonException("Failed to deserialize 'sequence-stop' value"))
                : throw new JsonException("'sequence-stop' command value cannot be null"),
            
            "sequence-reset" => firstProperty.Value is not null
                ? new SequenceResetCommand(firstProperty.Value.Deserialize<string>(options) 
                    ?? throw new JsonException("Failed to deserialize 'sequence-reset' value"))
                : throw new JsonException("'sequence-reset' command value cannot be null"),
            
            _ => throw new JsonException($"Unrecognized object discriminator key: '{propertyKey}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, SceneCommand value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

