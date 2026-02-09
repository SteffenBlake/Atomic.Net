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
        return new FlexMarginLeftBehavior(reader.GetSingle());
    }

    public override void Write(Utf8JsonWriter writer, FlexMarginLeftBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
