using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexPaddingBottomBehavior that allows direct float values.
/// Converts JSON "flexPaddingBottom": 10 to new FlexPaddingBottomBehavior(10).
/// </summary>
public class FlexPaddingBottomBehaviorConverter : JsonConverter<FlexPaddingBottomBehavior>
{
    public override FlexPaddingBottomBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexPaddingBottomBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexPaddingBottomBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexPaddingBottomBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
