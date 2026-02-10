using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexPaddingBottomBehavior that allows direct float values.
/// Converts JSON "flexPaddingBottom": 10 to new FlexPaddingBottomBehavior(10).
/// </summary>
public class FlexPaddingBottomBehaviorConverter : JsonConverter<FlexPaddingBottomBehavior>
{
    public override FlexPaddingBottomBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new FlexPaddingBottomBehavior(reader.GetSingle());
    }

    public override void Write(Utf8JsonWriter writer, FlexPaddingBottomBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
