using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Scenes;

public class SceneCommandConverter : JsonConverter<SceneCommand>
{
    public override SceneCommand Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonNode = JsonNode.Parse(ref reader, new() {
            PropertyNameCaseInsensitive = false
        });

        if (jsonNode is not JsonObject jsonObject)
        {
            throw new JsonException("Expected JSON object");
        }

        var firstProperty = jsonObject.First();
        var propertyKey = firstProperty.Key.ToLower();

        return propertyKey switch
        {
            "mut" => ParseMutCommand(firstProperty.Value),
            _ => throw new JsonException($"Unrecognized object discriminator key: '{propertyKey}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, SceneCommand value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    private static MutCommand ParseMutCommand(JsonNode? mutValue)
    {
        if (mutValue is null)
        {
            throw new JsonException("'mut' command value cannot be null");
        }

        if (mutValue is not JsonArray mutArray)
        {
            throw new JsonException("Expected 'mut' to be an array");
        }

        var operations = new MutOperation[mutArray.Count];
        
        for (var i = 0; i < mutArray.Count; i++)
        {
            var element = mutArray[i];
            
            if (element is not JsonObject operationObject)
            {
                throw new JsonException($"Expected mutation operation to be an object at index {i}");
            }

            var target = operationObject["target"];
            if (target is null)
            {
                throw new JsonException($"Missing 'target' property in mutation operation at index {i}");
            }

            var value = operationObject["value"];
            if (value is null)
            {
                throw new JsonException($"Missing 'value' property in mutation operation at index {i}");
            }

            operations[i] = new MutOperation(target, value);
        }

        return new MutCommand(operations);
    }
}

