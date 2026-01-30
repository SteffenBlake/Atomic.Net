using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Converts JSON flex height targets into JsonFlexHeightTarget.
/// </summary>
public class JsonFlexHeightTargetConverter : JsonConverter<JsonFlexHeightTarget>
{
    public override JsonFlexHeightTarget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonNode = JsonNode.Parse(ref reader);
        
        if (jsonNode is not JsonObject jsonObject)
        {
            throw new JsonException("FlexHeight target must be an object");
        }

        var firstProperty = jsonObject.First();
        var fieldName = firstProperty.Value?.GetValue<string>() 
            ?? throw new JsonException("FlexHeight field must have a string value");

        return new JsonFlexHeightTarget(fieldName);
    }

    public override void Write(Utf8JsonWriter writer, JsonFlexHeightTarget value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
