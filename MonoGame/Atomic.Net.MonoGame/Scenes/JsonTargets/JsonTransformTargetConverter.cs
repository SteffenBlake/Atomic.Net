using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Converts JSON transform targets into JsonTransformTarget variants.
/// </summary>
public class JsonTransformTargetConverter : JsonConverter<JsonTransformTarget>
{
    public override JsonTransformTarget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonNode = JsonNode.Parse(ref reader);

        if (jsonNode is not JsonObject jsonObject)
        {
            throw new JsonException("Transform target must be an object");
        }

        var firstProperty = jsonObject.First();
        var propertyKey = firstProperty.Key.ToLower();
        var fieldName = firstProperty.Value?.GetValue<string>() 
            ?? throw new JsonException($"'{propertyKey}' target must have a string value");

        return propertyKey switch
        {
            "position" => new JsonTransformTarget(new JsonTransformPositionTarget(fieldName)),
            "rotation" => new JsonTransformTarget(new JsonTransformRotationTarget(fieldName)),
            "scale" => new JsonTransformTarget(new JsonTransformScaleTarget(fieldName)),
            "anchor" => new JsonTransformTarget(new JsonTransformAnchorTarget(fieldName)),
            _ => throw new JsonException($"Unrecognized transform field: '{propertyKey}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, JsonTransformTarget value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
