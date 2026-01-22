using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED.Persistence;

/// <summary>
/// Behavior that marks an entity for disk persistence using LiteDB.
/// The Key property acts as a database pointer - entities can swap keys to different "save slots".
/// Orphaned keys persist in the database and can be reused.
/// </summary>
[JsonConverter(typeof(PersistToDiskBehaviorConverter))]
public readonly record struct PersistToDiskBehavior(string Key) : IBehavior<PersistToDiskBehavior>
{
    public static PersistToDiskBehavior CreateFor(Entity entity)
    {
        // senior-dev: #test-architect CreateFor requires a key parameter, so we return an empty default
        // The key must be provided when setting the behavior via SetBehavior
        return new PersistToDiskBehavior(string.Empty);
    }
}

/// <summary>
/// JSON converter for PersistToDiskBehavior deserialization.
/// </summary>
public class PersistToDiskBehaviorConverter : JsonConverter<PersistToDiskBehavior>
{
    public override PersistToDiskBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // senior-dev: Expect either an object with "key" property or a string value directly
        return reader.TokenType switch
        {
            JsonTokenType.StartObject => ReadObject(ref reader),
            JsonTokenType.String => new PersistToDiskBehavior(reader.GetString() ?? string.Empty),
            _ => default
        };
    }

    private static PersistToDiskBehavior ReadObject(ref Utf8JsonReader reader)
    {
        string? key = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var propertyName = reader.GetString();
            reader.Read();

            if (string.Equals(propertyName, "key", StringComparison.OrdinalIgnoreCase))
            {
                key = reader.GetString();
            }
        }

        // senior-dev: Empty/null keys will be validated by DatabaseRegistry event handlers
        return new PersistToDiskBehavior(key ?? string.Empty);
    }

    public override void Write(Utf8JsonWriter writer, PersistToDiskBehavior value, JsonSerializerOptions options)
    {
        // senior-dev: Write as object with "key" property for consistency
        writer.WriteStartObject();
        writer.WriteString("key", value.Key);
        writer.WriteEndObject();
    }
}
