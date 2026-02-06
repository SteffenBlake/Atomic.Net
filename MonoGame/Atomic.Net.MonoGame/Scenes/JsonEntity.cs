using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Properties;
using Atomic.Net.MonoGame.Transform;
using Atomic.Net.MonoGame.Persistence;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Hierarchy;
using Atomic.Net.MonoGame.Ids;
using Atomic.Net.MonoGame.Tags;
using Atomic.Net.MonoGame.Flex;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// JSON model for a single entity in a scene.
/// Reference type is acceptable as these are ephemeral (load-time only).
/// All behavior properties are nullable (optional in JSON).
/// </summary>
public class JsonEntity
{
    /// <summary>
    /// Optional enabled state for the entity. Defaults to true if not specified.
    /// When false, entity is spawned but immediately disabled.
    /// </summary>
    public bool? Enabled { get; set; } = null;

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

    // Flex behavior properties
    public FlexDirectionBehavior? FlexDirection { get; set; } = null;
    public FlexWidthBehavior? FlexWidth { get; set; } = null;
    public FlexHeightBehavior? FlexHeight { get; set; } = null;
    public FlexGrowBehavior? FlexGrow { get; set; } = null;
    public FlexShrinkBehavior? FlexShrink { get; set; } = null;
    public FlexWrapBehavior? FlexWrap { get; set; } = null;
    public FlexJustifyContentBehavior? FlexJustifyContent { get; set; } = null;
    public FlexAlignItemsBehavior? FlexAlignItems { get; set; } = null;
    public FlexAlignSelfBehavior? FlexAlignSelf { get; set; } = null;
    public AlignContentBehavior? AlignContent { get; set; } = null;
    public FlexPositionTypeBehavior? FlexPositionType { get; set; } = null;
    public FlexPositionLeftBehavior? FlexPositionLeft { get; set; } = null;
    public FlexPositionRightBehavior? FlexPositionRight { get; set; } = null;
    public FlexPositionTopBehavior? FlexPositionTop { get; set; } = null;
    public FlexPositionBottomBehavior? FlexPositionBottom { get; set; } = null;
    public FlexMarginLeftBehavior? FlexMarginLeft { get; set; } = null;
    public FlexMarginRightBehavior? FlexMarginRight { get; set; } = null;
    public FlexMarginTopBehavior? FlexMarginTop { get; set; } = null;
    public FlexMarginBottomBehavior? FlexMarginBottom { get; set; } = null;
    public FlexPaddingLeftBehavior? FlexPaddingLeft { get; set; } = null;
    public FlexPaddingRightBehavior? FlexPaddingRight { get; set; } = null;
    public FlexPaddingTopBehavior? FlexPaddingTop { get; set; } = null;
    public FlexPaddingBottomBehavior? FlexPaddingBottom { get; set; } = null;
    public FlexBorderLeftBehavior? FlexBorderLeft { get; set; } = null;
    public FlexBorderRightBehavior? FlexBorderRight { get; set; } = null;
    public FlexBorderTopBehavior? FlexBorderTop { get; set; } = null;
    public FlexBorderBottomBehavior? FlexBorderBottom { get; set; } = null;
    public FlexZOverride? FlexZOverride { get; set; } = null;

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

        // FIRST: Check if ANY flex behaviors exist, and add FlexBehavior BEFORE applying properties
        if (FlexDirection.HasValue || FlexWidth.HasValue || FlexHeight.HasValue ||
            FlexGrow.HasValue || FlexShrink.HasValue || FlexWrap.HasValue ||
            FlexJustifyContent.HasValue || FlexAlignItems.HasValue || FlexAlignSelf.HasValue ||
            AlignContent.HasValue || FlexPositionType.HasValue ||
            FlexPositionLeft.HasValue || FlexPositionRight.HasValue ||
            FlexPositionTop.HasValue || FlexPositionBottom.HasValue ||
            FlexMarginLeft.HasValue || FlexMarginRight.HasValue ||
            FlexMarginTop.HasValue || FlexMarginBottom.HasValue ||
            FlexPaddingLeft.HasValue || FlexPaddingRight.HasValue ||
            FlexPaddingTop.HasValue || FlexPaddingBottom.HasValue ||
            FlexBorderLeft.HasValue || FlexBorderRight.HasValue ||
            FlexBorderTop.HasValue || FlexBorderBottom.HasValue ||
            FlexZOverride.HasValue)
        {
            if (!entity.HasBehavior<FlexBehavior>())
            {
                entity.SetBehavior<FlexBehavior>(static (ref _) => { });
            }
        }

        // THEN: Apply individual flex behaviors
        if (FlexDirection.HasValue)
        {
            var input = FlexDirection.Value;
            entity.SetBehavior<FlexDirectionBehavior, FlexDirectionBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexWidth.HasValue)
        {
            var input = FlexWidth.Value;
            entity.SetBehavior<FlexWidthBehavior, FlexWidthBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexHeight.HasValue)
        {
            var input = FlexHeight.Value;
            entity.SetBehavior<FlexHeightBehavior, FlexHeightBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexGrow.HasValue)
        {
            var input = FlexGrow.Value;
            entity.SetBehavior<FlexGrowBehavior, FlexGrowBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexShrink.HasValue)
        {
            var input = FlexShrink.Value;
            entity.SetBehavior<FlexShrinkBehavior, FlexShrinkBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexWrap.HasValue)
        {
            var input = FlexWrap.Value;
            entity.SetBehavior<FlexWrapBehavior, FlexWrapBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexJustifyContent.HasValue)
        {
            var input = FlexJustifyContent.Value;
            entity.SetBehavior<FlexJustifyContentBehavior, FlexJustifyContentBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexAlignItems.HasValue)
        {
            var input = FlexAlignItems.Value;
            entity.SetBehavior<FlexAlignItemsBehavior, FlexAlignItemsBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexAlignSelf.HasValue)
        {
            var input = FlexAlignSelf.Value;
            entity.SetBehavior<FlexAlignSelfBehavior, FlexAlignSelfBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (AlignContent.HasValue)
        {
            var input = AlignContent.Value;
            entity.SetBehavior<AlignContentBehavior, AlignContentBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexPositionType.HasValue)
        {
            var input = FlexPositionType.Value;
            entity.SetBehavior<FlexPositionTypeBehavior, FlexPositionTypeBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexPositionLeft.HasValue)
        {
            var input = FlexPositionLeft.Value;
            entity.SetBehavior<FlexPositionLeftBehavior, FlexPositionLeftBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexPositionRight.HasValue)
        {
            var input = FlexPositionRight.Value;
            entity.SetBehavior<FlexPositionRightBehavior, FlexPositionRightBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexPositionTop.HasValue)
        {
            var input = FlexPositionTop.Value;
            entity.SetBehavior<FlexPositionTopBehavior, FlexPositionTopBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexPositionBottom.HasValue)
        {
            var input = FlexPositionBottom.Value;
            entity.SetBehavior<FlexPositionBottomBehavior, FlexPositionBottomBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexMarginLeft.HasValue)
        {
            var input = FlexMarginLeft.Value;
            entity.SetBehavior<FlexMarginLeftBehavior, FlexMarginLeftBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexMarginRight.HasValue)
        {
            var input = FlexMarginRight.Value;
            entity.SetBehavior<FlexMarginRightBehavior, FlexMarginRightBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexMarginTop.HasValue)
        {
            var input = FlexMarginTop.Value;
            entity.SetBehavior<FlexMarginTopBehavior, FlexMarginTopBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexMarginBottom.HasValue)
        {
            var input = FlexMarginBottom.Value;
            entity.SetBehavior<FlexMarginBottomBehavior, FlexMarginBottomBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexPaddingLeft.HasValue)
        {
            var input = FlexPaddingLeft.Value;
            entity.SetBehavior<FlexPaddingLeftBehavior, FlexPaddingLeftBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexPaddingRight.HasValue)
        {
            var input = FlexPaddingRight.Value;
            entity.SetBehavior<FlexPaddingRightBehavior, FlexPaddingRightBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexPaddingTop.HasValue)
        {
            var input = FlexPaddingTop.Value;
            entity.SetBehavior<FlexPaddingTopBehavior, FlexPaddingTopBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexPaddingBottom.HasValue)
        {
            var input = FlexPaddingBottom.Value;
            entity.SetBehavior<FlexPaddingBottomBehavior, FlexPaddingBottomBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexBorderLeft.HasValue)
        {
            var input = FlexBorderLeft.Value;
            entity.SetBehavior<FlexBorderLeftBehavior, FlexBorderLeftBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexBorderRight.HasValue)
        {
            var input = FlexBorderRight.Value;
            entity.SetBehavior<FlexBorderRightBehavior, FlexBorderRightBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexBorderTop.HasValue)
        {
            var input = FlexBorderTop.Value;
            entity.SetBehavior<FlexBorderTopBehavior, FlexBorderTopBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexBorderBottom.HasValue)
        {
            var input = FlexBorderBottom.Value;
            entity.SetBehavior<FlexBorderBottomBehavior, FlexBorderBottomBehavior>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }

        if (FlexZOverride.HasValue)
        {
            var input = FlexZOverride.Value;
            entity.SetBehavior<Flex.FlexZOverride, Flex.FlexZOverride>(
                in input,
                static (ref readonly _input, ref behavior) => behavior = _input
            );
        }
    }
}
