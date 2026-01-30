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
            "properties" => new JsonPropertiesTarget(
                firstProperty.Value?.GetValue<string>() 
                ?? throw new JsonException("'properties' target must have a string value")
            ),
            "position" or "rotation" or "scale" or "anchor" => 
                DeserializeTransformTarget(jsonObject, options),
            _ => throw new JsonException($"Unrecognized object target: '{propertyKey}'")
        };
    }

    private static JsonTransformFieldTarget DeserializeTransformTarget(JsonObject jsonObject, JsonSerializerOptions options)
    {
        var result = jsonObject.Deserialize<JsonTransformFieldTarget>(options);
        if (result.Equals(default(JsonTransformFieldTarget)))
        {
            throw new JsonException("Failed to deserialize transform target");
        }
        return result;
    }

    public override void Write(Utf8JsonWriter writer, JsonTarget value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
