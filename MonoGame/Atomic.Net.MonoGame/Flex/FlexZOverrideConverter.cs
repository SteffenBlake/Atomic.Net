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
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexZOverride(reader.GetInt32());
        }
        throw new JsonException($"Expected number for FlexZOverride, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexZOverride value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.ZIndex);
    }
}
