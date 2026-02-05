using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Sequencing;
using Json.More;

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

        KeyValuePair<string, JsonNode?>? firstProperty =
            jsonObject.Count == 0 ? null :
            jsonObject.FirstOrDefault();

        var propertyKey = firstProperty?.Key;

        SceneCommand? result = propertyKey == null ? null : propertyKey switch
        {
            "mut" => jsonNode.Deserialize<MutCommand>(options),
            "sequenceStart" => jsonNode.Deserialize<SequenceStartCommand>(options),
            "sequenceStop" => jsonNode.Deserialize<SequenceStopCommand>(options),
            "sequenceReset" => jsonNode.Deserialize<SequenceResetCommand>(options),
            "sceneLoad" => jsonNode.Deserialize<SceneLoadCommand>(options),
            _ => null
        };

        return result ?? throw new JsonException($"Unexpected value for '{propertyKey}': {firstProperty?.Value}");
    }

    public override void Write(Utf8JsonWriter writer, SceneCommand value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

