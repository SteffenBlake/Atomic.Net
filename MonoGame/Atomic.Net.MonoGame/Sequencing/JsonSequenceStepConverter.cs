using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Scenes;

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

        // Check for delay step
        if (jsonObject.TryGetPropertyValue("delay", out var delayNode) && delayNode != null)
        {
            var duration = delayNode.Deserialize<float>(options);
            return new DelayStep(duration);
        }

        // Check for do step
        if (jsonObject.TryGetPropertyValue("do", out var doNode) && doNode != null)
        {
            var command = doNode.Deserialize<SceneCommand>(options);
            return new DoStep(command);
        }

        // Check for tween step
        if (jsonObject.TryGetPropertyValue("tween", out var tweenNode) && tweenNode != null)
        {
            var tweenStep = tweenNode.Deserialize<TweenStep>(options);
            return tweenStep;
        }

        // Check for repeat step
        if (jsonObject.TryGetPropertyValue("repeat", out var repeatNode) && repeatNode != null)
        {
            var repeatStep = repeatNode.Deserialize<RepeatStep>(options);
            return repeatStep;
        }

        throw new JsonException("Unrecognized sequence step type. Expected one of: delay, do, tween, repeat");
    }

    public override void Write(Utf8JsonWriter writer, JsonSequenceStep value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
