using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexMarginRightBehavior that allows direct float values.
/// Converts JSON "flexMarginRight": 10 to new FlexMarginRightBehavior(10).
/// </summary>
public class FlexMarginRightBehaviorConverter : JsonConverter<FlexMarginRightBehavior>
{
    public override FlexMarginRightBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexMarginRightBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexMarginRightBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexMarginRightBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
