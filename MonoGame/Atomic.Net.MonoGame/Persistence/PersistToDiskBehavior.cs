using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;

namespace Atomic.Net.MonoGame.Persistence;

/// <summary>
/// Behavior that marks an entity for disk persistence using LiteDB.
/// The Key property acts as a database pointer - entities can swap keys to different "save slots".
/// Orphaned keys persist in the database and can be reused.
/// </summary>
public readonly record struct PersistToDiskBehavior(string Key) : 
    IBehavior<PersistToDiskBehavior>
{
        return new PersistToDiskBehavior(string.Empty);
    }
}
