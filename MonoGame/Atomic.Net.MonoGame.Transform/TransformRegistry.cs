using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Transform;

public sealed class TransformRegistry :
    ISingleton<TransformRegistry>,
    IEventHandler<BehaviorAddedEvent<PositionBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<PositionBehavior>>,
    IEventHandler<BehaviorRemovedEvent<PositionBehavior>>,
    IEventHandler<BehaviorAddedEvent<RotationBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<RotationBehavior>>,
    IEventHandler<BehaviorRemovedEvent<RotationBehavior>>,
    IEventHandler<BehaviorAddedEvent<ScaleBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<ScaleBehavior>>,
    IEventHandler<BehaviorRemovedEvent<ScaleBehavior>>,
    IEventHandler<BehaviorAddedEvent<AnchorBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<AnchorBehavior>>,
    IEventHandler<BehaviorRemovedEvent<AnchorBehavior>>,
    IEventHandler<BehaviorAddedEvent<Parent>>,
    IEventHandler<PostBehaviorUpdatedEvent<Parent>>,
    IEventHandler<BehaviorRemovedEvent<Parent>>
{
    public static TransformRegistry Instance { get; } = new();

    private readonly SparseArray<bool> _dirty = new(Constants.MaxEntities);

    public void Recalculate()
    {
        foreach(var dirty in _dirty)
        {
            if (!dirty.Value)
            {
                continue;
            }

            RecalculateNode(EntityRegistry.Instance[dirty.Index]);
        }
        _dirty.Clear();
    }

    private void RecalculateNode(Entity entity)
    {
        _dirty.Set(entity.Index, false);
        var parentTransform = Matrix.Identity;
        if (entity.TryGetParent(out var parent))
        {
            if (BehaviorRegistry<WorldTransform>.Instance.TryGetBehavior(
                    parent.Value, out var pTransform
            ))
            {
                parentTransform = pTransform.Value.Transform;
            }
        }

        var position = Vector3.Zero;
        if (BehaviorRegistry<PositionBehavior>.Instance.TryGetBehavior(entity, out var pos))
        {
            position = new Vector3(pos.Value.X, pos.Value.Y, pos.Value.Z);
        }

        var rotation = Quaternion.Identity;
        if (BehaviorRegistry<RotationBehavior>.Instance.TryGetBehavior(entity, out var rot))
        {
            rotation = new Quaternion(rot.Value.X, rot.Value.Y, rot.Value.Z, rot.Value.W);
        }

        var scale = Vector3.One;
        if (BehaviorRegistry<ScaleBehavior>.Instance.TryGetBehavior(entity, out var scl))
        {
            scale = new Vector3(scl.Value.X, scl.Value.Y, scl.Value.Z);
        }

        var anchor = Vector3.Zero;
        if (BehaviorRegistry<AnchorBehavior>.Instance.TryGetBehavior(entity, out var anc))
        {
            anchor = new Vector3(anc.Value.X, anc.Value.Y, anc.Value.Z);
        }

        // Compute local transform: Scale * Rotation * Translation * Anchor offset
        var localTransform =
            Matrix.CreateTranslation(-anchor) *
            Matrix.CreateScale(scale) *
            Matrix.CreateFromQuaternion(rotation) *
            Matrix.CreateTranslation(position + anchor);

        var worldTransform = localTransform * parentTransform;

        entity.SetBehavior<WorldTransform>((ref WorldTransform wt) =>
            wt = new WorldTransform(worldTransform)
        );

        foreach (var child in entity.GetChildren())
        {
            RecalculateNode(child);
        }
    }

    private void MarkDirty(Entity entity)
    {
        _dirty.Set(entity.Index, true);
    }

    public void OnEvent(BehaviorAddedEvent<PositionBehavior> e) => MarkDirty(e.Entity);
    public void OnEvent(PostBehaviorUpdatedEvent<PositionBehavior> e) => MarkDirty(e.Entity);
    public void OnEvent(BehaviorRemovedEvent<PositionBehavior> e) => MarkDirty(e.Entity);

    public void OnEvent(BehaviorAddedEvent<RotationBehavior> e) => MarkDirty(e.Entity);
    public void OnEvent(PostBehaviorUpdatedEvent<RotationBehavior> e) => MarkDirty(e.Entity);
    public void OnEvent(BehaviorRemovedEvent<RotationBehavior> e) => MarkDirty(e.Entity);

    public void OnEvent(BehaviorAddedEvent<ScaleBehavior> e) => MarkDirty(e.Entity);
    public void OnEvent(PostBehaviorUpdatedEvent<ScaleBehavior> e) => MarkDirty(e.Entity);
    public void OnEvent(BehaviorRemovedEvent<ScaleBehavior> e) => MarkDirty(e.Entity);

    public void OnEvent(BehaviorAddedEvent<AnchorBehavior> e) => MarkDirty(e.Entity);
    public void OnEvent(PostBehaviorUpdatedEvent<AnchorBehavior> e) => MarkDirty(e.Entity);
    public void OnEvent(BehaviorRemovedEvent<AnchorBehavior> e) => MarkDirty(e.Entity);

    public void OnEvent(BehaviorAddedEvent<Parent> e) => MarkDirty(e.Entity);
    public void OnEvent(PostBehaviorUpdatedEvent<Parent> e) => MarkDirty(e.Entity);
    public void OnEvent(BehaviorRemovedEvent<Parent> e) => MarkDirty(e.Entity);

    public static void Register()
    {
        EventBus<BehaviorAddedEvent<PositionBehavior>>.Register<TransformRegistry>();
        EventBus<PostBehaviorUpdatedEvent<PositionBehavior>>.Register<TransformRegistry>();
        EventBus<BehaviorRemovedEvent<PositionBehavior>>.Register<TransformRegistry>();

        EventBus<BehaviorAddedEvent<RotationBehavior>>.Register<TransformRegistry>();
        EventBus<PostBehaviorUpdatedEvent<RotationBehavior>>.Register<TransformRegistry>();
        EventBus<BehaviorRemovedEvent<RotationBehavior>>.Register<TransformRegistry>();

        EventBus<BehaviorAddedEvent<ScaleBehavior>>.Register<TransformRegistry>();
        EventBus<PostBehaviorUpdatedEvent<ScaleBehavior>>.Register<TransformRegistry>();
        EventBus<BehaviorRemovedEvent<ScaleBehavior>>.Register<TransformRegistry>();

        EventBus<BehaviorAddedEvent<AnchorBehavior>>.Register<TransformRegistry>();
        EventBus<PostBehaviorUpdatedEvent<AnchorBehavior>>.Register<TransformRegistry>();
        EventBus<BehaviorRemovedEvent<AnchorBehavior>>.Register<TransformRegistry>();

        EventBus<BehaviorAddedEvent<Parent>>.Register<TransformRegistry>();
        EventBus<PostBehaviorUpdatedEvent<Parent>>.Register<TransformRegistry>();
        EventBus<BehaviorRemovedEvent<Parent>>.Register<TransformRegistry>();
    }
}

