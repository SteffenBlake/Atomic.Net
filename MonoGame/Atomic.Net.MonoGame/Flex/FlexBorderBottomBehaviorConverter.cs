using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexBorderBottomBehavior that allows direct float values.
/// Converts JSON "flexBorderBottom": 10 to new FlexBorderBottomBehavior(10).
/// </summary>
public class FlexBorderBottomBehaviorConverter : JsonConverter<FlexBorderBottomBehavior>
{
    public override FlexBorderBottomBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new FlexBorderBottomBehavior(reader.GetSingle());
    }

    public override void Write(Utf8JsonWriter writer, FlexBorderBottomBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
