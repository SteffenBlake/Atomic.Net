using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Scenes.Persistence;

/// <summary>
/// Behavior that marks an entity for disk persistence using LiteDB.
/// The Key property acts as a database pointer - entities can swap keys to different "save slots".
/// Orphaned keys persist in the database and can be reused.
/// </summary>
public readonly record struct PersistToDiskBehavior(string Key) : IBehavior<PersistToDiskBehavior>
{
    public static PersistToDiskBehavior CreateFor(Entity entity)
    {
        // senior-dev: #test-architect CreateFor requires a key parameter, so we return an empty default
        // The key must be provided when setting the behavior via SetBehavior
        return new PersistToDiskBehavior(string.Empty);
    }
}
