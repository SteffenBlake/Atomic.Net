using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Transform;
using Atomic.Net.MonoGame.Hierarchy;
using System.Drawing;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Registry that computes world-space flex rectangles from WorldTransform + FlexBehavior.
/// Runs AFTER TransformRegistry.Recalculate() to ensure world transforms are up-to-date.
/// </summary>
public sealed class WorldFlexRegistry :
    ISingleton<WorldFlexRegistry>,
    IEventHandler<InitializeEvent>,
    IEventHandler<BehaviorAddedEvent<FlexBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<FlexBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<FlexBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<WorldTransformBehavior>>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
        EventBus<InitializeEvent>.Register(Instance);
    }

    public static WorldFlexRegistry Instance { get; private set; } = null!;

    private readonly PartitionedSparseArray<bool> _dirty = new(
        Constants.MaxGlobalEntities,
        Constants.MaxSceneEntities
    );

    private readonly PartitionedSparseArray<bool> _worldFlexUpdated = new(
        Constants.MaxGlobalEntities,
        Constants.MaxSceneEntities
    );

    public void OnEvent(InitializeEvent _)
    {
        EventBus<BehaviorAddedEvent<FlexBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<FlexBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<FlexBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<WorldTransformBehavior>>.Register(this);
    }

    public void Recalculate()
    {
        // Process global entities
        foreach (var (entityIndex, _) in _dirty.Global)
        {
            ProcessDirtyEntity((ushort)entityIndex);
        }

        // Process scene entities
        foreach (var (entityIndex, _) in _dirty.Scene)
        {
            ProcessDirtyEntity((uint)entityIndex);
        }

        // Clear dirty flags
        _dirty.Global.Clear();
        _dirty.Scene.Clear();

        // Fire bulk events
        FireBulkEvents();
    }

    private void ProcessDirtyEntity(PartitionIndex entityIndex)
    {
        var entity = EntityRegistry.Instance[entityIndex];

        if (!BehaviorRegistry<FlexBehavior>.Instance.TryGetBehavior(entity, out var flexBehavior))
        {
            return;
        }

        if (!BehaviorRegistry<WorldTransformBehavior>.Instance.TryGetBehavior(entity, out var worldTransform))
        {
            return;
        }

        // Get world position from world transform
        var worldPosition = worldTransform.Value.Value.Translation;

        // Transform local flex rectangles to world space
        var marginRect = TransformRectToWorld(
            flexBehavior.Value.MarginRect,
            worldPosition.X,
            worldPosition.Y
        );
        var paddingRect = TransformRectToWorld(
            flexBehavior.Value.PaddingRect,
            worldPosition.X,
            worldPosition.Y
        );
        var contentRect = TransformRectToWorld(
            flexBehavior.Value.ContentRect,
            worldPosition.X,
            worldPosition.Y
        );

        var worldFlex = new WorldFlexBehavior(
            MarginRect: marginRect,
            PaddingRect: paddingRect,
            ContentRect: contentRect,
            BorderLeft: flexBehavior.Value.BorderLeft,
            BorderTop: flexBehavior.Value.BorderTop,
            BorderRight: flexBehavior.Value.BorderRight,
            BorderBottom: flexBehavior.Value.BorderBottom,
            ZIndex: flexBehavior.Value.ZIndex
        );

        entity.SetBehavior<WorldFlexBehavior, WorldFlexBehavior>(
            in worldFlex,
            static (ref readonly input, ref output) => output = input
        );

        _worldFlexUpdated.Set(entityIndex, true);
    }

    private static RectangleF TransformRectToWorld(RectangleF localRect, float worldX, float worldY)
    {
        return new RectangleF(
            localRect.X + worldX,
            localRect.Y + worldY,
            localRect.Width,
            localRect.Height
        );
    }

    private void FireBulkEvents()
    {
        foreach (var (index, _) in _worldFlexUpdated.Global)
        {
            var entity = EntityRegistry.Instance[(ushort)index];
            EventBus<PostBehaviorUpdatedEvent<WorldFlexBehavior>>.Push(new(entity));
        }

        foreach (var (index, _) in _worldFlexUpdated.Scene)
        {
            var entity = EntityRegistry.Instance[(uint)index];
            EventBus<PostBehaviorUpdatedEvent<WorldFlexBehavior>>.Push(new(entity));
        }

        _worldFlexUpdated.Global.Clear();
        _worldFlexUpdated.Scene.Clear();
    }

    private void MarkDirty(Entity entity)
    {
        _dirty.Set(entity.Index, true);
    }

    public void OnEvent(BehaviorAddedEvent<FlexBehavior> e)
    {
        e.Entity.SetBehavior<WorldFlexBehavior>(static (ref _) => { });
        MarkDirty(e.Entity);
    }

    public void OnEvent(PostBehaviorUpdatedEvent<FlexBehavior> e) => MarkDirty(e.Entity);

    public void OnEvent(PreBehaviorRemovedEvent<FlexBehavior> e)
    {
        BehaviorRegistry<WorldFlexBehavior>.Instance.Remove(e.Entity);
        _dirty.Remove(e.Entity.Index);
        _worldFlexUpdated.Remove(e.Entity.Index);
    }

    public void OnEvent(PostBehaviorUpdatedEvent<WorldTransformBehavior> e)
    {
        // When world transform updates, recalculate world flex if entity has flex
        if (e.Entity.HasBehavior<FlexBehavior>())
        {
            MarkDirty(e.Entity);
        }
    }
}
