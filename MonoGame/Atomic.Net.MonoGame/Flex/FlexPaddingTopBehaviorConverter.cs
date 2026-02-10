using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexPaddingTopBehavior that allows direct float values.
/// Converts JSON "flexPaddingTop": 10 to new FlexPaddingTopBehavior(10).
/// </summary>
public class FlexPaddingTopBehaviorConverter : JsonConverter<FlexPaddingTopBehavior>
{
    public override FlexPaddingTopBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new FlexPaddingTopBehavior(reader.GetSingle());
    }

    public override void Write(Utf8JsonWriter writer, FlexPaddingTopBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
