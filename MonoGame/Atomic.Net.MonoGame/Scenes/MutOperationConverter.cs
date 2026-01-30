using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Scenes.JsonTargets;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Custom JSON converter for MutOperation that validates target and value properties
/// are present and non-null during deserialization.
/// </summary>
public class MutOperationConverter : JsonConverter<MutOperation>
{
    public override MutOperation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonNode = JsonNode.Parse(ref reader);
        
        if (jsonNode is not JsonObject jsonObject)
        {
            throw new JsonException("Expected mutation operation to be an object");
        }

        var targetNode = jsonObject["target"];
        if (targetNode is null)
        {
            throw new JsonException("Missing 'target' property in mutation operation");
        }

        var value = jsonObject["value"];
        if (value is null)
        {
            throw new JsonException("Missing 'value' property in mutation operation");
        }

        // Deserialize target using JsonTargetConverter
        var target = targetNode.Deserialize<JsonTarget>(options);

        return new MutOperation(target, value);
    }

    public override void Write(Utf8JsonWriter writer, MutOperation value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
