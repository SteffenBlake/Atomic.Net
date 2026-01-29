using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Scenes;

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
            var command = doNode.Deserialize<SceneCommand>(options) 
                ?? throw new JsonException("Failed to deserialize 'do' command");
            return new DoStep(command);
        }

        if (jsonObject.TryGetPropertyValue("tween", out var tweenNode) && tweenNode is JsonObject tweenObj)
        {
            var from = tweenObj["from"]?.Deserialize<float>(options) 
                ?? throw new JsonException("Tween step missing 'from' field");
            var to = tweenObj["to"]?.Deserialize<float>(options) 
                ?? throw new JsonException("Tween step missing 'to' field");
            var duration = tweenObj["duration"]?.Deserialize<float>(options) 
                ?? throw new JsonException("Tween step missing 'duration' field");
            var doCmd = tweenObj["do"]?.Deserialize<SceneCommand>(options) 
                ?? throw new JsonException("Tween step missing 'do' field");
            
            return new TweenStep(from, to, duration, doCmd);
        }

        if (jsonObject.TryGetPropertyValue("repeat", out var repeatNode) && repeatNode is JsonObject repeatObj)
        {
            var every = repeatObj["every"]?.Deserialize<float>(options) 
                ?? throw new JsonException("Repeat step missing 'every' field");
            var until = repeatObj["until"]?.AsObject() 
                ?? throw new JsonException("Repeat step missing 'until' field");
            var doCmd = repeatObj["do"]?.Deserialize<SceneCommand>(options) 
                ?? throw new JsonException("Repeat step missing 'do' field");
            
            return new RepeatStep(every, until, doCmd);
        }

        throw new JsonException("Unrecognized sequence step type. Expected 'delay', 'do', 'tween', or 'repeat'");
    }

    public override void Write(Utf8JsonWriter writer, JsonSequenceStep value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
