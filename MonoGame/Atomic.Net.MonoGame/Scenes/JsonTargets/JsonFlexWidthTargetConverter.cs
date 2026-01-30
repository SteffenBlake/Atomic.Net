using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

public class JsonFlexWidthTargetConverter : JsonConverter<JsonFlexWidthTarget>
{
    public override JsonFlexWidthTarget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonNode = JsonNode.Parse(ref reader);
        
        if (jsonNode is not JsonObject jsonObject)
        {
            throw new JsonException("FlexWidth target must be an object");
        }

        var firstProperty = jsonObject.First();
        var fieldName = firstProperty.Value?.GetValue<string>() 
            ?? throw new JsonException("FlexWidth field must have a string value");

        return new JsonFlexWidthTarget(fieldName);
    }

    public override void Write(Utf8JsonWriter writer, JsonFlexWidthTarget value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
