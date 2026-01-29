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
        if (jsonObject.TryGetPropertyValue("tween", out var tweenNode) && tweenNode is JsonObject tweenObj)
        {
            if (tweenObj["from"] == null)
            {
                throw new JsonException("Tween step missing 'from' field");
            }
            
            if (tweenObj["to"] == null)
            {
                throw new JsonException("Tween step missing 'to' field");
            }
            
            if (tweenObj["duration"] == null)
            {
                throw new JsonException("Tween step missing 'duration' field");
            }
            
            if (tweenObj["do"] == null)
            {
                throw new JsonException("Tween step missing 'do' field");
            }
            
            var from = tweenObj["from"]!.Deserialize<float>(options);
            var to = tweenObj["to"]!.Deserialize<float>(options);
            var duration = tweenObj["duration"]!.Deserialize<float>(options);
            var doCommand = tweenObj["do"]!.Deserialize<SceneCommand>(options);
            
            return new TweenStep(from, to, duration, doCommand);
        }

        // Check for repeat step
        if (jsonObject.TryGetPropertyValue("repeat", out var repeatNode) && repeatNode is JsonObject repeatObj)
        {
            if (repeatObj["every"] == null)
            {
                throw new JsonException("Repeat step missing 'every' field");
            }
            
            if (repeatObj["until"] == null)
            {
                throw new JsonException("Repeat step missing 'until' field");
            }
            
            if (repeatObj["do"] == null)
            {
                throw new JsonException("Repeat step missing 'do' field");
            }
            
            var every = repeatObj["every"]!.Deserialize<float>(options);
            var until = repeatObj["until"]!;
            var doCommand = repeatObj["do"]!.Deserialize<SceneCommand>(options);
            
            return new RepeatStep(every, until, doCommand);
        }

        throw new JsonException("Unrecognized sequence step type. Expected one of: delay, do, tween, repeat");
    }

    public override void Write(Utf8JsonWriter writer, JsonSequenceStep value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
