using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexMarginBottomBehavior that allows direct float values.
/// Converts JSON "flexMarginBottom": 10 to new FlexMarginBottomBehavior(10).
/// </summary>
public class FlexMarginBottomBehaviorConverter : JsonConverter<FlexMarginBottomBehavior>
{
    public override FlexMarginBottomBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new FlexMarginBottomBehavior(reader.GetSingle());
    }

    public override void Write(Utf8JsonWriter writer, FlexMarginBottomBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
