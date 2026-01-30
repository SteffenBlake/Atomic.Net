using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Converts JSON transform field targets into JsonTransformFieldTarget variants.
/// </summary>
public class JsonTransformFieldTargetConverter : JsonConverter<JsonTransformFieldTarget>
{
    public override JsonTransformFieldTarget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonNode = JsonNode.Parse(ref reader);

        if (jsonNode is not JsonObject jsonObject)
        {
            throw new JsonException("Transform field target must be an object");
        }

        var firstProperty = jsonObject.First();
        var propertyKey = firstProperty.Key.ToLower();
        var fieldName = firstProperty.Value?.GetValue<string>() 
            ?? throw new JsonException($"'{propertyKey}' target must have a string value");

        return propertyKey switch
        {
            "position" => new JsonTransformFieldTarget(new JsonTransformPositionTarget(fieldName)),
            "rotation" => new JsonTransformFieldTarget(new JsonTransformRotationTarget(fieldName)),
            "scale" => new JsonTransformFieldTarget(new JsonTransformScaleTarget(fieldName)),
            "anchor" => new JsonTransformFieldTarget(new JsonTransformAnchorTarget(fieldName)),
            _ => throw new JsonException($"Unrecognized transform field: '{propertyKey}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, JsonTransformFieldTarget value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
