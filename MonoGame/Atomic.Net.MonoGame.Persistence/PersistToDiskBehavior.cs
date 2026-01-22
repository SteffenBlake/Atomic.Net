using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;

namespace Atomic.Net.MonoGame.Persistence;

/// <summary>
/// Behavior that marks an entity for disk persistence using LiteDB.
/// The Key property acts as a database pointer - entities can swap keys to different "save slots".
/// Orphaned keys persist in the database and can be reused.
/// </summary>
/// <remarks>
/// test-architect: This is an API stub. Implementation will be done by @senior-dev.
/// Key behavior:
/// - Empty/null keys should fire ErrorEvent and skip persistence
/// - Key changes trigger database load/write operations
/// - oldKey == newKey check prevents infinite loops during deserialization
/// </remarks>
[JsonConverter(typeof(PersistToDiskBehaviorConverter))]
public readonly record struct PersistToDiskBehavior(string Key) : IBehavior<PersistToDiskBehavior>
{
    public static PersistToDiskBehavior CreateFor(Entity entity)
    {
        // test-architect: To be implemented by @senior-dev
        throw new NotImplementedException("PersistToDiskBehavior.CreateFor - to be implemented by @senior-dev");
    }
}

/// <summary>
/// JSON converter for PersistToDiskBehavior deserialization.
/// </summary>
/// <remarks>
/// test-architect: This is an API stub. Implementation will be done by @senior-dev.
/// </remarks>
public class PersistToDiskBehaviorConverter : JsonConverter<PersistToDiskBehavior>
{
    public override PersistToDiskBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // test-architect: To be implemented by @senior-dev
        throw new NotImplementedException("PersistToDiskBehaviorConverter.Read - to be implemented by @senior-dev");
    }

    public override void Write(Utf8JsonWriter writer, PersistToDiskBehavior value, JsonSerializerOptions options)
    {
        // test-architect: To be implemented by @senior-dev
        throw new NotImplementedException("PersistToDiskBehaviorConverter.Write - to be implemented by @senior-dev");
    }
}
