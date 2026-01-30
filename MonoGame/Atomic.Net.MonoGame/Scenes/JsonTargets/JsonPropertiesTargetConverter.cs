using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Converts JSON property targets ("propertyKey" string) into JsonPropertiesTarget.
/// </summary>
public class JsonPropertiesTargetConverter : JsonConverter<JsonPropertiesTarget>
{
    public override JsonPropertiesTarget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonNode = JsonNode.Parse(ref reader);

        if (jsonNode is not JsonValue jsonValue || !jsonValue.TryGetValue<string>(out var propertyKey))
        {
            throw new JsonException("Properties target must be a string");
        }

        return new JsonPropertiesTarget(propertyKey);
    }

    public override void Write(Utf8JsonWriter writer, JsonPropertiesTarget value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
