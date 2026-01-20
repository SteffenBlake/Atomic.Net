using Atomic.Net.MonoGame.Transform;

namespace Atomic.Net.MonoGame.Scenes.JsonModels;

/// <summary>
/// JSON model for a single entity in a scene.
/// Reference type is acceptable as these are ephemeral (load-time only).
/// All behavior properties are nullable (optional in JSON).
/// </summary>
public class JsonEntity
{
    /// <summary>
    /// Optional entity ID for referencing (e.g., "player", "main-menu").
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Optional transform behavior.
    /// Missing fields use C# defaults (Position/Anchor: Zero, Rotation: Identity, Scale: One).
    /// </summary>
    public TransformBehavior? Transform { get; set; }

    /// <summary>
    /// Optional parent reference (e.g., "#player" to reference entity with id="player").
    /// </summary>
    public string? Parent { get; set; }
}
