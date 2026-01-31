using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Sequencing;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Custom JSON converter for SceneCommand that handles discriminator-based deserialization.
/// Supports 'mut', 'sequenceStart', 'sequenceStop', and 'sequenceReset' command types.
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
        var propertyKey = firstProperty.Key;
        
        SceneCommand? result = propertyKey switch
        {
            "mut" when firstProperty.Value is not null => 
                new MutCommand(firstProperty.Value.Deserialize<MutOperation[]>(options)!),
            "sequenceStart" when firstProperty.Value is not null => 
                new SequenceStartCommand(firstProperty.Value.Deserialize<string>(options)!),
            "sequenceStop" when firstProperty.Value is not null => 
                new SequenceStopCommand(firstProperty.Value.Deserialize<string>(options)!),
            "sequenceReset" when firstProperty.Value is not null => 
                new SequenceResetCommand(firstProperty.Value.Deserialize<string>(options)!),
            _ => null
        };

        return result ?? throw new JsonException($"Unexpected value for '{propertyKey}': {firstProperty.Value}");
    }

    public override void Write(Utf8JsonWriter writer, SceneCommand value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

