using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;

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

        const int maxIterations = 100;

        _worldTransformUpdated.Clear();

        // Step 1: SIMD compute LocalTransform from Position/Rotation/Scale/Anchor
        LocalTransformBlockMapSet.Instance.Recalculate();

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            if (_dirty.Count == 0)
            {
                break;
            }

            _dirty.Clear();

            foreach (var (idx, _) in _dirty)
            {
                _worldTransformUpdated.Set(idx, true);
            }

            // Step 2: SIMD compute WorldTransform = LocalTransform × ParentWorldTransform
            WorldTransformBlockMapSet.Instance.Recalculate();

            // Step 3: Scatter - copy WorldTransform to children's ParentWorldTransform
            ScatterToChildren();
        }

        // Step 5: Fire bulk events
        FireBulkEvents();
    }

    private void ScatterToChildren()
    {
        foreach (var (parentIdx, _) in _dirty)
        {
            var children = HierarchyRegistry.Instance.GetChildrenArray(parentIdx);
            if (children == null)
            {
                continue;
            }

            foreach (var (childIdx, _) in children)
            {
                // Copy only the 12 computed elements
                ParentWorldTransformBackingStore.Instance.M11.Set(
                    childIdx, WorldTransformBackingStore.M11[parentIdx] ?? 1f
                );
                ParentWorldTransformBackingStore.Instance.M12.Set(
                    childIdx, WorldTransformBackingStore.M12[parentIdx] ?? 0f
                );
                ParentWorldTransformBackingStore.Instance.M13.Set(
                    childIdx, WorldTransformBackingStore.M13[parentIdx] ?? 0f
                );
                ParentWorldTransformBackingStore.Instance.M21.Set(
                    childIdx, WorldTransformBackingStore.M21[parentIdx] ?? 0f
                );
                ParentWorldTransformBackingStore.Instance.M22.Set(
                    childIdx, WorldTransformBackingStore.M22[parentIdx] ?? 1f
                );
                ParentWorldTransformBackingStore.Instance.M23.Set(
                    childIdx, WorldTransformBackingStore.M23[parentIdx] ?? 0f
                );
                ParentWorldTransformBackingStore.Instance.M31.Set(
                    childIdx, WorldTransformBackingStore.M31[parentIdx] ?? 0f
                );
                ParentWorldTransformBackingStore.Instance.M32.Set(
                    childIdx, WorldTransformBackingStore.M32[parentIdx] ?? 0f
                );
                ParentWorldTransformBackingStore.Instance.M33.Set(
                    childIdx, WorldTransformBackingStore.M33[parentIdx] ?? 1f
                );
                ParentWorldTransformBackingStore.Instance.M41.Set(
                    childIdx, WorldTransformBackingStore.M41[parentIdx] ?? 0f
                );
                ParentWorldTransformBackingStore.Instance.M42.Set(
                    childIdx, WorldTransformBackingStore.M42[parentIdx] ?? 0f
                );
                ParentWorldTransformBackingStore.Instance.M43.Set(
                    childIdx, WorldTransformBackingStore.M43[parentIdx] ?? 0f
                );

                _dirty.Set(childIdx, true);
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
        // Initialize WorldTransformBehavior and backing stores
        // Note: TransformBackingStore is already initialized by the behavior creation,
        // and user values have already been set via the mutate function
        ParentWorldTransformBackingStore.Instance.SetupForEntity(e.Entity);
        e.Entity.SetRefBehavior<WorldTransformBehavior>();
        
        MarkDirty(e.Entity);
    }

    public void OnEvent(PostBehaviorUpdatedEvent<TransformBehavior> e) => MarkDirty(e.Entity);

    public void OnEvent(PreBehaviorRemovedEvent<TransformBehavior> e)
    {
        CleanupForEntity(e.Entity);
        // Keep entity marked dirty to ensure proper recomputation on next allocation
        MarkDirty(e.Entity);
    }

    /// <summary>
    /// Cleans up all transform-related backing store data for an entity.
    /// </summary>
    private void CleanupForEntity(Entity entity)
    {
        TransformBackingStore.Instance.CleanupForEntity(entity);
        ParentWorldTransformBackingStore.Instance.CleanupForEntity(entity);
        _worldTransformUpdated.Remove(entity.Index);
    }

    public void OnEvent(BehaviorAddedEvent<Parent> e) => MarkDirty(e.Entity);
    public void OnEvent(PostBehaviorUpdatedEvent<Parent> e) => MarkDirty(e.Entity);
    public void OnEvent(PreBehaviorRemovedEvent<Parent> e) => MarkDirty(e.Entity);
}

