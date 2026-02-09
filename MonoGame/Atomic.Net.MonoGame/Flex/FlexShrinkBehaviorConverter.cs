using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexShrinkBehavior that allows direct float values.
/// Converts JSON "flexShrink": 2.0 to new FlexShrinkBehavior(2.0f).
/// </summary>
public class FlexShrinkBehaviorConverter : JsonConverter<FlexShrinkBehavior>
{
    public override FlexShrinkBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexShrinkBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexShrinkBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexShrinkBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
