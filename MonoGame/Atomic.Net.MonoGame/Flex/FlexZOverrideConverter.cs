using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexZOverride that allows direct int values.
/// Converts JSON "flexZOverride": 10 to new FlexZOverride(10).
/// </summary>
public class FlexZOverrideConverter : JsonConverter<FlexZOverride>
{
    public override FlexZOverride Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new FlexZOverride(reader.GetInt32());
    }

    public override void Write(Utf8JsonWriter writer, FlexZOverride value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.ZIndex);
    }
}
