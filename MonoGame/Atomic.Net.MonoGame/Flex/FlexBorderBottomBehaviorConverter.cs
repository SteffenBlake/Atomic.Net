using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexBorderBottomBehavior that allows direct float values.
/// Converts JSON "flexBorderBottom": 10 to new FlexBorderBottomBehavior(10).
/// </summary>
public class FlexBorderBottomBehaviorConverter : JsonConverter<FlexBorderBottomBehavior>
{
    public override FlexBorderBottomBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexBorderBottomBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexBorderBottomBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexBorderBottomBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
