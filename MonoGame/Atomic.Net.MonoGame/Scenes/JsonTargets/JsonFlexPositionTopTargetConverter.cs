using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

public class JsonFlexPositionTopTargetConverter : JsonConverter<JsonFlexPositionTopTarget>
{
    public override JsonFlexPositionTopTarget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonNode = JsonNode.Parse(ref reader);
        
        if (jsonNode is not JsonObject jsonObject)
        {
            throw new JsonException("FlexPositionTop target must be an object");
        }

        var firstProperty = jsonObject.First();
        var fieldName = firstProperty.Value?.GetValue<string>() 
            ?? throw new JsonException("FlexPositionTop field must have a string value");

        return new JsonFlexPositionTopTarget(fieldName);
    }

    public override void Write(Utf8JsonWriter writer, JsonFlexPositionTopTarget value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
