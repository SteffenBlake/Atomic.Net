using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexBorderTopBehavior that allows direct float values.
/// Converts JSON "flexBorderTop": 10 to new FlexBorderTopBehavior(10).
/// </summary>
public class FlexBorderTopBehaviorConverter : JsonConverter<FlexBorderTopBehavior>
{
    public override FlexBorderTopBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexBorderTopBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexBorderTopBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexBorderTopBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
