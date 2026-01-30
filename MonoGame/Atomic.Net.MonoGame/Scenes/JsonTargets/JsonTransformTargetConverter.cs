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

        return propertyKey switch
        {
            "position" => jsonNode.Deserialize<JsonTransformPositionTarget>(options),
            "rotation" => jsonNode.Deserialize<JsonTransformRotationTarget>(options),
            "scale" => jsonNode.Deserialize<JsonTransformScaleTarget>(options),
            "anchor" => jsonNode.Deserialize<JsonTransformAnchorTarget>(options),
            _ => throw new JsonException($"Unrecognized transform field: '{propertyKey}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, JsonTransformTarget value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
