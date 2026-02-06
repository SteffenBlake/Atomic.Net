using System.Text.Json;
using System.Text.Json.Serialization;
using FlexLayoutSharp;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexDirectionBehavior that handles string format.
/// Accepts "row", "column", "rowReverse", "columnReverse"
/// </summary>
public class FlexDirectionBehaviorConverter : JsonConverter<FlexDirectionBehavior>
{
    public override FlexDirectionBehavior Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (Enum.TryParse<FlexDirection>(value, true, out var direction))
            {
                return new FlexDirectionBehavior(direction);
            }
            throw new JsonException($"Invalid FlexDirection value: {value}");
        }

        throw new JsonException($"Expected string for FlexDirection, got {reader.TokenType}");
    }

    public override void Write(
        Utf8JsonWriter writer,
        FlexDirectionBehavior value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value.ToString());
    }
}
