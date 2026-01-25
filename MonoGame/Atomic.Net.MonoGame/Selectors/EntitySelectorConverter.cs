using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Selectors;

// TODO: @senior-dev This will now all be deleted, use EntitySelector.TryParse instead

public class EntitySelectorConverter : JsonConverter<EntitySelector>
{
    public override EntitySelector Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options
    )
    {
        var nextString = reader.GetString()?.Trim();
       
        if (EntitySelector.TryParse(nextString, out var selector))
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

