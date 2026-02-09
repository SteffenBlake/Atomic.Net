using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexBorderLeftBehavior that allows direct float values.
/// Converts JSON "flexBorderLeft": 10 to new FlexBorderLeftBehavior(10).
/// </summary>
public class FlexBorderLeftBehaviorConverter : JsonConverter<FlexBorderLeftBehavior>
{
    public override FlexBorderLeftBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexBorderLeftBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexBorderLeftBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexBorderLeftBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
