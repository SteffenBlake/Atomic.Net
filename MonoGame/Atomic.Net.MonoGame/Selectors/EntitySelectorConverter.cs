using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Selectors;

public class EntitySelectorConverter : JsonConverter<EntitySelector>
{
    public override EntitySelector Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options
    )
    {
        var nextString = reader.GetString()?.Trim();

        if (SelectorRegistry.Instance.TryParse(nextString, out var selector))
        {
            return selector;
        }

        throw new JsonException("Unable to parse EntitySelector");
    }

    public override void Write(Utf8JsonWriter writer, EntitySelector value, JsonSerializerOptions options)
    {
        var output = value.ToString();
        writer.WriteStringValue(output);
    }
}

public class NullableEntitySelectorConverter : JsonConverter<EntitySelector?>
{
    public override EntitySelector? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var nextString = reader.GetString()?.Trim();

        if (SelectorRegistry.Instance.TryParse(nextString, out var selector))
        {
            return selector;
        }

        throw new JsonException("Unable to parse EntitySelector");
    }

    public override void Write(Utf8JsonWriter writer, EntitySelector? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var output = value.ToString();
        writer.WriteStringValue(output);
    }
}

