using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexPaddingLeftBehavior that allows direct float values.
/// Converts JSON "flexPaddingLeft": 10 to new FlexPaddingLeftBehavior(10).
/// </summary>
public class FlexPaddingLeftBehaviorConverter : JsonConverter<FlexPaddingLeftBehavior>
{
    public override FlexPaddingLeftBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new FlexPaddingLeftBehavior(reader.GetSingle());
    }

    public override void Write(Utf8JsonWriter writer, FlexPaddingLeftBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
