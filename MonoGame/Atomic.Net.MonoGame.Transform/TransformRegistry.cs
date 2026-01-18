using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Transform;

public sealed class TransformRegistry :
    ISingleton<TransformRegistry>,
    IEventHandler<InitializeEvent>,
    IEventHandler<BehaviorAddedEvent<TransformBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<TransformBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<TransformBehavior>>,
    IEventHandler<BehaviorAddedEvent<Parent>>,
    IEventHandler<PostBehaviorUpdatedEvent<Parent>>,
    IEventHandler<PreBehaviorRemovedEvent<Parent>>
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

    public static TransformRegistry Instance { get; private set; } = null!;

    private SparseArray<bool> _dirty = new(Constants.MaxEntities);
    private SparseArray<bool> _nextDirty = new(Constants.MaxEntities);
    private readonly SparseArray<bool> _worldTransformUpdated = new(Constants.MaxEntities);
    private readonly SparseArray<Matrix> _worldTransforms = new(Constants.MaxEntities);

    public void OnEvent(InitializeEvent _)
    {
        EventBus<BehaviorAddedEvent<TransformBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<TransformBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<TransformBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<Parent>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<Parent>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<Parent>>.Register(this);
    }

    private Vector3 _localAnchorNegV = default;
    private Matrix _localAnchorNeg = default;
    private Matrix _localScale = default;
    private Matrix _localRotation = default;
    private Matrix _localAnchor = default;
    private Matrix _localpos = default;
    private Matrix _localTransform = default;

    public void Recalculate()
    {
        // Infinite loop protection
        const int maxIterations = 100;
        for(var iterations = 1; iterations <= maxIterations; iterations++)
        {
            foreach(var (entityIndex, _) in _dirty)
            {
                var entity = EntityRegistry.Instance[entityIndex];

                // Skip if any ancestor is dirty - they will mark us dirty again after they process
                if (HasDirtyAncestor(entity))
                {
                    continue;
                }

                if (!BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity, out var transform))
                {
                    continue;
                }

                var anchor = transform.Value.Anchor;
                var scale = transform.Value.Scale;
                var rotation = transform.Value.Rotation;
                var position = transform.Value.Position;

                Vector3.Multiply(ref anchor, -1, out _localAnchorNegV);

                Matrix.CreateTranslation(ref _localAnchorNegV, out _localAnchorNeg);
                Matrix.CreateScale(ref scale, out _localScale);
                Matrix.CreateFromQuaternion(ref rotation, out _localRotation);
                Matrix.CreateTranslation(ref anchor, out _localAnchor);
                Matrix.CreateTranslation(ref position, out _localpos);

                Matrix.Multiply(ref _localAnchorNeg, ref _localScale, out _localTransform);
                Matrix.Multiply(ref _localTransform, ref _localRotation, out _localTransform);
                Matrix.Multiply(ref _localTransform, ref _localAnchor, out _localTransform);
                Matrix.Multiply(ref _localTransform, ref _localpos, out _localTransform);

                // Get parent world transform
                Matrix parentWorldTransform = Matrix.Identity;
                if (entity.TryGetParent(out var parent))
                {
                    if (_worldTransforms.TryGetValue(parent.Value.Index, out var parentWorld))
                    {
                        parentWorldTransform = parentWorld.Value;
                    }
                }

                // Calculate and store world transform directly
                Matrix.Multiply(ref _localTransform, ref parentWorldTransform, out var worldTransform);
                _worldTransforms.Set(entityIndex, worldTransform);

                // Use GetChildrenArray for allocation-free iteration
                var childrenArray = HierarchyRegistry.Instance.GetChildrenArray(entityIndex);
                if (childrenArray != null)
                {
                    foreach (var (childIndex, _) in childrenArray)
                    {
                        _nextDirty.Set(childIndex, true);
                    }
                }

                _worldTransformUpdated.Set(entityIndex, true);
            }

            if (_nextDirty.Count <= 0)
            {
                break;
            }

            _dirty.Clear();
            (_dirty, _nextDirty) = (_nextDirty, _dirty);
        }

        // Fire bulk events
        FireBulkEvents();
    }

    private void FireBulkEvents()
    {
        // Sync world transforms to behavior registry and fire events
        foreach (var (index, _) in _worldTransformUpdated)
        {
            var entity = EntityRegistry.Instance[index];
            if (_worldTransforms.TryGetValue(index, out var worldTransform))
            {
                BehaviorRegistry<WorldTransformBehavior>.Instance.SetBehavior(
                    entity,
                    (ref wt) => wt.Value = worldTransform.Value
                );
            }
        }

        _worldTransformUpdated.Clear();
    }

    private bool HasDirtyAncestor(Entity entity)
    {
        var current = entity;
        while (current.TryGetParent(out var parent))
        {
            if (_dirty.HasValue(parent.Value.Index))
            {
                return true;
            }
            current = parent.Value;
        }
        return false;
    }

    private void MarkDirty(Entity entity)
    {
        _dirty.Set(entity.Index, true);
    }

    public void OnEvent(BehaviorAddedEvent<TransformBehavior> e)
    {
        BehaviorRegistry<WorldTransformBehavior>.Instance.SetBehavior(
            e.Entity,
            (ref _) => { }
        );
        MarkDirty(e.Entity);
    }

    public void OnEvent(PostBehaviorUpdatedEvent<TransformBehavior> e) => MarkDirty(e.Entity);

    public void OnEvent(PreBehaviorRemovedEvent<TransformBehavior> e)
    {
        BehaviorRegistry<WorldTransformBehavior>.Instance.Remove(e.Entity);
        _dirty.Remove(e.Entity.Index);
        _worldTransformUpdated.Remove(e.Entity.Index);
    }

    public void OnEvent(BehaviorAddedEvent<Parent> e) => MarkDirty(e.Entity);
    public void OnEvent(PostBehaviorUpdatedEvent<Parent> e) => MarkDirty(e.Entity);
    public void OnEvent(PreBehaviorRemovedEvent<Parent> e) => MarkDirty(e.Entity);
}
