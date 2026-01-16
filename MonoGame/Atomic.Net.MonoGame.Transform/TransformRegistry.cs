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
            if (RefBehaviorRegistry<WorldTransform>.Instance.TryGetBehavior(
                    parent.Value, out var pTransform
            ))
            {
                parentTransform = pTransform.Value.Value.AsMatrix();
            }
        }

        var position = Vector3.Zero;
        if (RefBehaviorRegistry<PositionBehavior>.Instance.TryGetBehavior(entity, out var pos))
        {
            position = pos.Value.Value.AsVector3();
        }

        var rotation = Quaternion.Identity;
        if (RefBehaviorRegistry<RotationBehavior>.Instance.TryGetBehavior(entity, out var rot))
        {
            rotation = rot.Value.Value.AsQuaternion();
        }

        var scale = Vector3.One;
        if (RefBehaviorRegistry<ScaleBehavior>.Instance.TryGetBehavior(entity, out var scl))
        {
            scale = scl.Value.Value.AsVector3();
        }

        var anchor = Vector3.Zero;
        if (RefBehaviorRegistry<AnchorBehavior>.Instance.TryGetBehavior(entity, out var anc))
        {
            anchor = anc.Value.Value.AsVector3();
        }

        // Compute local transform: Scale * Rotation * Translation * Anchor offset
        var localTransform =
            Matrix.CreateTranslation(-anchor) *
            Matrix.CreateScale(scale) *
            Matrix.CreateFromQuaternion(rotation) *
            Matrix.CreateTranslation(position + anchor);

        var worldTransform = localTransform * parentTransform;

        RefBehaviorRegistry<WorldTransform>.Instance.SetBehavior(entity, (ref readonly WorldTransform wt) =>
        {
            wt.Value.M11.Value = worldTransform.M11;
            wt.Value.M12.Value = worldTransform.M12;
            wt.Value.M13.Value = worldTransform.M13;
            wt.Value.M14.Value = worldTransform.M14;
            wt.Value.M21.Value = worldTransform.M21;
            wt.Value.M22.Value = worldTransform.M22;
            wt.Value.M23.Value = worldTransform.M23;
            wt.Value.M24.Value = worldTransform.M24;
            wt.Value.M31.Value = worldTransform.M31;
            wt.Value.M32.Value = worldTransform.M32;
            wt.Value.M33.Value = worldTransform.M33;
            wt.Value.M34.Value = worldTransform.M34;
            wt.Value.M41.Value = worldTransform.M41;
            wt.Value.M42.Value = worldTransform.M42;
            wt.Value.M43.Value = worldTransform.M43;
            wt.Value.M44.Value = worldTransform.M44;
        });

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

