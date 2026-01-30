using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

public class JsonFlexPositionBottomTargetConverter : JsonConverter<JsonFlexPositionBottomTarget>
{
    public override JsonFlexPositionBottomTarget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonNode = JsonNode.Parse(ref reader);
        
        if (jsonNode is not JsonObject jsonObject)
        {
            throw new JsonException("FlexPositionBottom target must be an object");
        }

        var firstProperty = jsonObject.First();
        var fieldName = firstProperty.Value?.GetValue<string>() 
            ?? throw new JsonException("FlexPositionBottom field must have a string value");

        return new JsonFlexPositionBottomTarget(fieldName);
    }

    public override void Write(Utf8JsonWriter writer, JsonFlexPositionBottomTarget value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
