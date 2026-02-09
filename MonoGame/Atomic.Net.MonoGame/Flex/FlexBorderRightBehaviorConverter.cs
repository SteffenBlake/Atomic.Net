using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexBorderRightBehavior that allows direct float values.
/// Converts JSON "flexBorderRight": 10 to new FlexBorderRightBehavior(10).
/// </summary>
public class FlexBorderRightBehaviorConverter : JsonConverter<FlexBorderRightBehavior>
{
    public override FlexBorderRightBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexBorderRightBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexBorderRightBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexBorderRightBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
