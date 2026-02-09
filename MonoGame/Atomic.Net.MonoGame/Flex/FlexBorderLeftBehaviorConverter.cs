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
        return new FlexBorderLeftBehavior(reader.GetSingle());
    }

    public override void Write(Utf8JsonWriter writer, FlexBorderLeftBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
