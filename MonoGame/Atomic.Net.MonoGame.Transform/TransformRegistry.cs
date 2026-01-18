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

    private readonly SparseArray<bool> _dirty = new(Constants.MaxEntities);
    private readonly SparseArray<bool> _worldTransformUpdated = new(Constants.MaxEntities);
    private readonly List<ushort> _dirtyIndices = new(Constants.MaxEntities);

    public void OnEvent(InitializeEvent _)
    {
        EventBus<BehaviorAddedEvent<TransformBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<TransformBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<TransformBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<Parent>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<Parent>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<Parent>>.Register(this);
    }

    public void Recalculate()
    {
        if (_dirty.Count == 0)
        {
            return;
        }

        _worldTransformUpdated.Clear();

        // Collect all dirty entities
        _dirtyIndices.Clear();
        foreach (var (index, _) in _dirty)
        {
            _dirtyIndices.Add(index);
            _worldTransformUpdated.Set(index, true);
        }
        _dirty.Clear();

        // Process each dirty entity using MonoGame Matrix helpers
        foreach (var entityIndex in _dirtyIndices)
        {
            var entity = EntityRegistry.Instance[entityIndex];

            if (!BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity, out var transform))
            {
                continue;
            }

            // Build local transform using MonoGame's Matrix.Create* methods
            // Order: translate to origin → scale → rotate → translate back to anchor → translate to position
            var localTransform =
                Matrix.CreateTranslation(-transform.Value.Anchor) *
                Matrix.CreateScale(transform.Value.Scale) *
                Matrix.CreateFromQuaternion(transform.Value.Rotation) *
                Matrix.CreateTranslation(transform.Value.Anchor) *
                Matrix.CreateTranslation(transform.Value.Position);

            // Get parent world transform
            Matrix parentWorldTransform = Matrix.Identity;
            if (entity.TryGetParent(out var parent))
            {
                if (BehaviorRegistry<WorldTransformBehavior>.Instance.TryGetBehavior(parent.Value, out var parentWorld))
                {
                    parentWorldTransform = parentWorld.Value.Value;
                }
            }

            // Compute world transform
            var worldTransform = localTransform * parentWorldTransform;

            // Update world transform behavior
            BehaviorRegistry<WorldTransformBehavior>.Instance.SetBehavior(
                entity,
                (ref WorldTransformBehavior wt) => wt.Value = worldTransform
            );
        }

        // Fire bulk events
        FireBulkEvents();
    }

    private void ScatterToChildren(List<ushort> parentIndices)
    {
        foreach (var parentIndex in parentIndices)
        {
            var children = HierarchyRegistry.Instance.GetChildrenArray(parentIndex);
            if (children == null)
            {
                continue;
            }

            foreach (var (childIndex, _) in children)
            {
                _dirty.Set(childIndex, true);
            }
        }
    }

    private void FireBulkEvents()
    {
        foreach (var (index, _) in _worldTransformUpdated)
        {
            var entity = EntityRegistry.Instance[index];
            EventBus<PostBehaviorUpdatedEvent<WorldTransformBehavior>>.Push(new(entity));
        }

        _worldTransformUpdated.Clear();
    }

    private void MarkDirty(Entity entity)
    {
        _dirty.Set(entity.Index, true);
    }

    public void OnEvent(BehaviorAddedEvent<TransformBehavior> e)
    {
        BehaviorRegistry<WorldTransformBehavior>.Instance.SetBehavior(
            e.Entity,
            (ref WorldTransformBehavior _) => { }
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
