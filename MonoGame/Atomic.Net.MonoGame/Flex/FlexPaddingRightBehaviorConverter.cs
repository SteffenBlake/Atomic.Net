using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexPaddingRightBehavior that allows direct float values.
/// Converts JSON "flexPaddingRight": 10 to new FlexPaddingRightBehavior(10).
/// </summary>
public class FlexPaddingRightBehaviorConverter : JsonConverter<FlexPaddingRightBehavior>
{
    public override FlexPaddingRightBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexPaddingRightBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexPaddingRightBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexPaddingRightBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
