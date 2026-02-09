using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexMarginTopBehavior that allows direct float values.
/// Converts JSON "flexMarginTop": 10 to new FlexMarginTopBehavior(10).
/// </summary>
public class FlexMarginTopBehaviorConverter : JsonConverter<FlexMarginTopBehavior>
{
    public override FlexMarginTopBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexMarginTopBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexMarginTopBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexMarginTopBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
