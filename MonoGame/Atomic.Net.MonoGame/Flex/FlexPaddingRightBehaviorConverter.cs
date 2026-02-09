using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexPaddingRightBehavior that allows direct float values.
/// Converts JSON "flexPaddingRight": 10 to new FlexPaddingRightBehavior(10).
/// </summary>
public class FlexPaddingRightBehaviorConverter : JsonConverter<FlexPaddingRightBehavior>
{
    public override FlexPaddingRightBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new FlexPaddingRightBehavior(reader.GetSingle());
    }

    public override void Write(Utf8JsonWriter writer, FlexPaddingRightBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
