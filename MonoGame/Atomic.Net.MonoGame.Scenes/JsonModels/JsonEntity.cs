using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Transform;

namespace Atomic.Net.MonoGame.Scenes.JsonModels;

/// <summary>
/// JSON model for a single entity in a scene.
/// Reference type is acceptable as these are ephemeral (load-time only).
/// All behavior properties are nullable (optional in JSON).
/// </summary>
public struct JsonEntity()
{
    /// <summary>
    /// Optional entity ID for referencing (e.g., "player", "main-menu").
    /// </summary>
    public IdBehavior? Id = null;

    /// <summary>
    /// Optional transform behavior.
    /// Missing fields use C# defaults (Position/Anchor: Zero, Rotation: Identity, Scale: One).
    /// </summary>
    public TransformBehavior? Transform = null;

    /// <summary>
    /// Optional parent reference (e.g., "#player" to reference entity with id="player").
    /// </summary>
    public EntitySelector? Parent = null;
}
