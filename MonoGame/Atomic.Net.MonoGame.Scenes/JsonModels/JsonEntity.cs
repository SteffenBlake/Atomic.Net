using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Properties;
using Atomic.Net.MonoGame.Transform;

namespace Atomic.Net.MonoGame.Scenes.JsonModels;

// SteffenBlake: @senior-dev, move this to .Atomic.Net.Monogame.Scenes/ parent dir and namespace

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
    public IdBehavior? Id { get; set; } = null;

    /// <summary>
    /// Optional transform behavior.
    /// Missing fields use C# defaults (Position/Anchor: Zero, Rotation: Identity, Scale: One).
    /// </summary>
    public TransformBehavior? Transform { get; set; } = null;

    /// <summary>
    /// Optional parent reference (e.g., "#player" to reference entity with id="player").
    /// </summary>
    public EntitySelector? Parent { get; set; } = null;

    /// <summary>
    /// Optional properties behavior (arbitrary key-value metadata).
    /// </summary>
    public PropertiesBehavior? Properties { get; set; } = null;
}
