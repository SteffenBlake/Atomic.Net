using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Scenes;

public class SceneCommandConverter : JsonConverter<SceneCommand>
{
    public override SceneCommand Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonNode = JsonNode.Parse(ref reader, new() {
            PropertyNameCaseInsensitive = false
        });

        if (jsonNode is not JsonObject jsonObject)
        {
            throw new JsonException("Expected JSON object");
        }

        var firstProperty = jsonObject.First();
        var propertyKey = firstProperty.Key.ToLower();

        return propertyKey switch
        {
            "mut" => jsonNode.Deserialize<MutCommand>(options),
            _ => throw new JsonException($"Unrecognized object discriminator key: '{propertyKey}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, SceneCommand value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

