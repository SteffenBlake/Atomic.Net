using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexMarginLeftBehavior that allows direct float values.
/// Converts JSON "flexMarginLeft": 10 to new FlexMarginLeftBehavior(10).
/// </summary>
public class FlexMarginLeftBehaviorConverter : JsonConverter<FlexMarginLeftBehavior>
{
    public override FlexMarginLeftBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexMarginLeftBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexMarginLeftBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexMarginLeftBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
