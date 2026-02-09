using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexGrowBehavior that allows direct float values.
/// Converts JSON "flexGrow": 1.5 to new FlexGrowBehavior(1.5f).
/// </summary>
public class FlexGrowBehaviorConverter : JsonConverter<FlexGrowBehavior>
{
    public override FlexGrowBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new FlexGrowBehavior(reader.GetSingle());
    }

    public override void Write(Utf8JsonWriter writer, FlexGrowBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
