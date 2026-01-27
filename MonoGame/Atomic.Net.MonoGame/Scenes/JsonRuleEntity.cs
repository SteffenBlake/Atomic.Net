using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Tags;
using Atomic.Net.MonoGame.Transform;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// JSON model for an entity when used in rules execution.
/// Includes _index property for mapping mutations back to entities.
/// Used for serialization to JsonNode for JsonLogic evaluation.
/// </summary>
public class JsonRuleEntity
{
    /// <summary>
    /// Entity index in EntityRegistry for mapping mutations back.
    /// </summary>
    public ushort _index { get; set; }
    
    /// <summary>
    /// Optional entity ID for referencing (e.g., "player", "main-menu").
    /// </summary>
    public string? id { get; set; } = null;
    
    /// <summary>
    /// Optional tags for group selection in rules and queries (e.g., ["enemy", "boss"]).
    /// </summary>
    public string[]? tags { get; set; } = null;

    /// <summary>
    /// Optional transform behavior.
    /// </summary>
    public TransformBehavior? transform { get; set; } = null;

    /// <summary>
    /// Optional world transform (calculated).
    /// </summary>
    public WorldTransformBehavior? worldTransform { get; set; } = null;

    /// <summary>
    /// Optional properties behavior (arbitrary key-value metadata).
    /// </summary>
    public PropertiesBehavior? properties { get; set; } = null;
}
