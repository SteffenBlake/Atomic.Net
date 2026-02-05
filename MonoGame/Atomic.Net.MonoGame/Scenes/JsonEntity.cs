using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Transform;
using Atomic.Net.MonoGame.Persistence;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Hierarchy;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Tags;

namespace Atomic.Net.MonoGame.Scenes;

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
    /// Optional tags for group selection in rules and queries (e.g., ["enemy", "boss"]).
    /// </summary>
    public TagsBehavior? Tags { get; set; } = null;

    /// <summary>
    /// Optional transform behavior.
    /// Missing fields use C# defaults (Position/Anchor: Zero, Rotation: Identity, Scale: One).
    /// </summary>
    public TransformBehavior? Transform { get; set; } = null;

    /// <summary>
    /// Optional parent reference (e.g., "@player" to reference entity with id="player").
    /// </summary>
    public ParentBehavior? Parent { get; set; } = null;

    /// <summary>
    /// Optional properties behavior (arbitrary key-value metadata).
    /// </summary>
    public PropertiesBehavior? Properties { get; set; } = null;

    /// <summary>
    /// Optional disk persistence behavior (marks entity for saving to LiteDB).
    /// MUST be applied LAST in SceneLoader to prevent unwanted DB loads during scene construction
    /// </summary>
    public PersistToDiskBehavior? PersistToDisk { get; set; } = null;

    /// <summary>
    /// Creates a JsonEntity from an existing entity by reading all its behaviors.
    /// Used for serialization to disk.
    /// </summary>
    public static void ReadFromEntity(Entity entity, ref JsonEntity jsonEntity)
    {
        // Collect all behaviors from their respective registries
        jsonEntity.Id = BehaviorRegistry<IdBehavior>.Instance.TryGetBehavior(
            entity, out var id
        ) ? id : null;

        jsonEntity.Tags = BehaviorRegistry<TagsBehavior>.Instance.TryGetBehavior(
            entity, out var tags
        ) ? tags : null;

        jsonEntity.Transform = BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(
            entity, out var transform
        ) ? transform : null;

        jsonEntity.Properties = BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(
            entity, out var properties
        ) ? properties : null;

        jsonEntity.PersistToDisk = BehaviorRegistry<PersistToDiskBehavior>.Instance.TryGetBehavior(
            entity, out var persistToDisk
        ) ? persistToDisk : null;

        // Handle Parent (stored in BehaviorRegistry<Parent>)
        if (BehaviorRegistry<ParentBehavior>.Instance.TryGetBehavior(entity, out var parentBehavior))
        {
            jsonEntity.Parent = parentBehavior;
        }
    }

    /// <summary>
    /// Writes all behaviors from this JsonEntity to an existing entity.
    /// Used for deserialization from disk.
    /// Does NOT apply PersistToDiskBehavior (already set by caller).
    /// </summary>
    public void WriteToEntity(Entity entity)
    {
        // Apply all behaviors from JSON (in specific order per sprint requirements)
        // Transform, Properties, Id, Tags applied first, then Parent
        // PersistToDiskBehavior is NOT applied here (already set by caller)

        if (Transform.HasValue)
        {
            var input = Transform.Value;
            entity.SetBehavior<TransformBehavior, TransformBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (Properties.HasValue)
        {
            var input = Properties.Value;
            entity.SetBehavior<PropertiesBehavior, PropertiesBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (Id.HasValue)
        {
            var input = Id.Value;
            entity.SetBehavior<IdBehavior, IdBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (Tags.HasValue)
        {
            var input = Tags.Value;
            entity.SetBehavior<TagsBehavior, TagsBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (Parent.HasValue)
        {
            var input = Parent.Value;
            entity.SetBehavior<ParentBehavior, ParentBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }
    }
}
