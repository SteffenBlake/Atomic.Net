using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.BED.JsonConverters;

public class IdBehaviorConverter : JsonConverter<IdBehavior>
{
    public override IdBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var nextString = reader.GetString()?.Trim();
        if (string.IsNullOrEmpty(nextString))
        {
            return default;
        }

        return new ()
        {
            Id = nextString
        };
    }

    public override void Write(Utf8JsonWriter writer, IdBehavior value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

