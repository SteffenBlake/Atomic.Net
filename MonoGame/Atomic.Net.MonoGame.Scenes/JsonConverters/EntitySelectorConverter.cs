using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Scenes.JsonModels;

namespace Atomic.Net.MonoGame.Scenes.JsonConverters;

// SteffenBlake: @senior-dev, move this to .Atomic.Net.Monogame.Scenes/ parent dir and namespace

public class EntitySelectorConverter : JsonConverter<EntitySelector>
{
    public override EntitySelector Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options
    )
    {
        var nextString = reader.GetString()?.Trim();
        if (string.IsNullOrEmpty(nextString))
        {
            return default;
        }

        if (nextString.StartsWith('#'))
        {
            return new ()
            {
                ById = nextString.TrimStart('#')
            };
        }

        // TODO: Future types of entity selectors

        return default;
    }

    public override void Write(Utf8JsonWriter writer, EntitySelector value, JsonSerializerOptions options)
    {
        if (!string.IsNullOrEmpty(value.ById))
        {
            writer.WriteStringValue($"#{value.ById}");
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

