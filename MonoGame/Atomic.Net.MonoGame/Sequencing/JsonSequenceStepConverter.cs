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
        var propertyKey = firstProperty.Key;

        JsonSequenceStep? result = firstProperty.Value is null ? null : propertyKey switch
        {
            "delay" =>
                firstProperty.Value.Deserialize<float>(options) is var duration
                    ? new DelayStep(duration)
                    : null,
            "do" =>
                firstProperty.Value.Deserialize<Scenes.SceneCommand>(options) is { } cmd
                    ? new DoStep(cmd)
                    : null,
            "tween" =>
                firstProperty.Value.Deserialize<TweenStep>(options),
            "repeat" =>
                firstProperty.Value.Deserialize<RepeatStep>(options),
            _ => null
        };

        return result ?? throw new JsonException($"Unexpected value for '{propertyKey}': {firstProperty.Value}");
    }

    public override void Write(Utf8JsonWriter writer, JsonSequenceStep value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
