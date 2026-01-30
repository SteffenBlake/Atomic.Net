using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Converts JSON target representations into strongly-typed JsonTarget variants.
/// Handles both string targets (e.g., "flexAlignItems") and object targets (e.g., { "properties": "health" }).
/// </summary>
public class JsonTargetConverter : JsonConverter<JsonTarget>
{
    public override JsonTarget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonNode = JsonNode.Parse(ref reader);

        // Handle string targets (id, tags, parent, flex behaviors)
        if (jsonNode is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var targetString))
        {
            return targetString;
        }

        // Handle object targets (properties, transform fields)
        if (jsonNode is not JsonObject jsonObject)
        {
            throw new JsonException("Target must be a string or object");
        }

        var firstProperty = jsonObject.First();
        var propertyKey = firstProperty.Key.ToLower();

        return propertyKey switch
        {
            "properties" => firstProperty.Value.Deserialize<JsonPropertiesTarget>(options),
            "position" or "rotation" or "scale" or "anchor" => 
                jsonObject.Deserialize<JsonTransformTarget>(options),
            _ => throw new JsonException($"Unrecognized object target: '{propertyKey}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, JsonTarget value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
