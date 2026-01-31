using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Custom JSON converter for JsonSequenceStep that handles discriminator-based deserialization.
/// Supports 'delay', 'do', 'tween', and 'repeat' step types.
/// </summary>
public class JsonSequenceStepConverter : JsonConverter<JsonSequenceStep>
{
    public override JsonSequenceStep Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonNode = JsonNode.Parse(ref reader);

        if (jsonNode is not JsonObject jsonObject)
        {
            throw new JsonException("Expected JSON object for sequence step");
        }

        var firstProperty = jsonObject.First();
        var propertyKey = firstProperty.Key.ToLower();

        return propertyKey switch
        {
            "delay" => firstProperty.Value is not null
                ? new DelayStep(
                    firstProperty.Value.Deserialize<float>(options))
                : throw new JsonException("'delay' value cannot be null"),

            "do" => firstProperty.Value is not null
                ? new DoStep(
                    firstProperty.Value.Deserialize<Scenes.SceneCommand>(options))
                : throw new JsonException("'do' value cannot be null"),

            "tween" => firstProperty.Value is not null
                ? firstProperty.Value.Deserialize<TweenStep>(options)
                : throw new JsonException("'tween' value cannot be null"),

            "repeat" => firstProperty.Value is not null
                ? firstProperty.Value.Deserialize<RepeatStep>(options)
                : throw new JsonException("'repeat' value cannot be null"),

            _ => throw new JsonException($"Unrecognized sequence step discriminator key: '{propertyKey}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, JsonSequenceStep value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
