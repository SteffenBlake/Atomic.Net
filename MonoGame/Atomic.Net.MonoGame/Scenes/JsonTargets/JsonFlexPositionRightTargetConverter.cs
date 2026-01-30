using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

public class JsonFlexPositionRightTargetConverter : JsonConverter<JsonFlexPositionRightTarget>
{
    public override JsonFlexPositionRightTarget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonNode = JsonNode.Parse(ref reader);
        
        if (jsonNode is not JsonObject jsonObject)
        {
            throw new JsonException("FlexPositionRight target must be an object");
        }

        var firstProperty = jsonObject.First();
        var fieldName = firstProperty.Value?.GetValue<string>() 
            ?? throw new JsonException("FlexPositionRight field must have a string value");

        return new JsonFlexPositionRightTarget(fieldName);
    }

    public override void Write(Utf8JsonWriter writer, JsonFlexPositionRightTarget value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
