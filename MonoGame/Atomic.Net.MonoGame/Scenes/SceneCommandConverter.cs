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
                firstProperty.Value.Deserialize<MutOperation[]>(options) is { } ops
                    ? new MutCommand(ops)
                    : null,
            "sequenceStart" when firstProperty.Value is not null => 
                firstProperty.Value.Deserialize<string>(options) is { } id
                    ? new SequenceStartCommand(id)
                    : null,
            "sequenceStop" when firstProperty.Value is not null => 
                firstProperty.Value.Deserialize<string>(options) is { } id
                    ? new SequenceStopCommand(id)
                    : null,
            "sequenceReset" when firstProperty.Value is not null => 
                firstProperty.Value.Deserialize<string>(options) is { } id
                    ? new SequenceResetCommand(id)
                    : null,
            _ => null
        };

        return result ?? throw new JsonException($"Unexpected value for '{propertyKey}': {firstProperty.Value}");
    }

    public override void Write(Utf8JsonWriter writer, SceneCommand value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

