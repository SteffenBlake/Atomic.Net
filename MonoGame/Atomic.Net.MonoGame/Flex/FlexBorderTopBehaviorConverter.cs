using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexBorderTopBehavior that allows direct float values.
/// Converts JSON "flexBorderTop": 10 to new FlexBorderTopBehavior(10).
/// </summary>
public class FlexBorderTopBehaviorConverter : JsonConverter<FlexBorderTopBehavior>
{
    public override FlexBorderTopBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new FlexBorderTopBehavior(reader.GetSingle());
    }

    public override void Write(Utf8JsonWriter writer, FlexBorderTopBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
