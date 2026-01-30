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
            return targetString switch
            {
                "id" => new JsonIdTarget(),
                "tags" => new JsonTagsTarget(),
                "parent" => new JsonParentTarget(),
                // All flex behaviors are string targets
                _ when targetString.StartsWith("flex") => new JsonFlexTarget(targetString),
                _ => throw new JsonException($"Unrecognized string target: '{targetString}'")
            };
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
            "position" => new JsonTransformFieldTarget(new JsonTransformPositionTarget(
                firstProperty.Value?.GetValue<string>() 
                ?? throw new JsonException("'position' target must have a string value")
            )),
            "rotation" => new JsonTransformFieldTarget(new JsonTransformRotationTarget(
                firstProperty.Value?.GetValue<string>() 
                ?? throw new JsonException("'rotation' target must have a string value")
            )),
            "scale" => new JsonTransformFieldTarget(new JsonTransformScaleTarget(
                firstProperty.Value?.GetValue<string>() 
                ?? throw new JsonException("'scale' target must have a string value")
            )),
            "anchor" => new JsonTransformFieldTarget(new JsonTransformAnchorTarget(
                firstProperty.Value?.GetValue<string>() 
                ?? throw new JsonException("'anchor' target must have a string value")
            )),
            _ => throw new JsonException($"Unrecognized object target: '{propertyKey}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, JsonTarget value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
