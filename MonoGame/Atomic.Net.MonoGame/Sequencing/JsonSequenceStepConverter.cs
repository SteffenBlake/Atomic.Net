using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Custom JSON converter for JsonSequenceStep variant type.
/// Deserializes based on discriminator fields: delay, do, tween, repeat.
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

        if (jsonObject.TryGetPropertyValue("delay", out var delayNode) && delayNode != null)
        {
            var duration = delayNode.Deserialize<float>(options);
            return new DelayStep(duration);
        }

        if (jsonObject.TryGetPropertyValue("do", out var doNode) && doNode != null)
        {
            var step = doNode.Deserialize<DoStep>(options);
            return step;
        }

        if (jsonObject.TryGetPropertyValue("tween", out var tweenNode) && tweenNode != null)
        {
            var step = tweenNode.Deserialize<TweenStep>(options);
            return step;
        }

        if (jsonObject.TryGetPropertyValue("repeat", out var repeatNode) && repeatNode != null)
        {
            var step = repeatNode.Deserialize<RepeatStep>(options);
            return step;
        }

        throw new JsonException("Unrecognized sequence step type. Expected 'delay', 'do', 'tween', or 'repeat'");
    }

    public override void Write(Utf8JsonWriter writer, JsonSequenceStep value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
