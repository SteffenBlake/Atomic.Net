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
    private readonly HashSet<ushort> _worldTransformUpdated = new(Constants.MaxEntities);

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
        const int maxIterations = 100;

        _worldTransformUpdated.Clear();

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            if (_dirty.Count == 0)
            {
                break;
            }

            var entitiesToProcess = _dirty.Select(d => d.Index).ToList();
            _dirty.Clear();

            foreach (var idx in entitiesToProcess)
            {
                _worldTransformUpdated.Add(idx);
            }

            // Step 1: SIMD compute LocalTransform from Position/Rotation/Scale/Anchor
            LocalTransformBlockMapSet.Instance.Recalculate();

            // Step 2: SIMD compute WorldTransform = LocalTransform × ParentWorldTransform
            WorldTransformBlockMapSet.Instance.Recalculate();

            // Step 3: Copy computed WorldTransform to WorldTransformBackingStore
            // (Only the 12 computed elements - M14, M24, M34, M44 are preset to constants)
            CopyWorldTransformToStore(entitiesToProcess);

            // Step 4: Scatter - copy WorldTransform to children's ParentWorldTransform
            ScatterToChildren(entitiesToProcess);
        }

        // Step 5: Fire bulk events
        FireBulkEvents();
    }

    private void CopyWorldTransformToStore(List<ushort> entities)
    {
        foreach (var index in entities)
        {
            // Only copy the 12 computed elements
            // M14, M24, M34, M44 are already preset to (0, 0, 0, 1) via initValue
            WorldTransformBackingStore.Instance.M11.Set(
                index, WorldTransformBlockMapSet.Instance.M11[index] ?? 1f
            );
            WorldTransformBackingStore.Instance.M12.Set(
                index, WorldTransformBlockMapSet.Instance.M12[index] ?? 0f
            );
            WorldTransformBackingStore.Instance.M13.Set(
                index, WorldTransformBlockMapSet.Instance.M13[index] ?? 0f
            );
            WorldTransformBackingStore.Instance.M21.Set(
                index, WorldTransformBlockMapSet.Instance.M21[index] ?? 0f
            );
            WorldTransformBackingStore.Instance.M22.Set(
                index, WorldTransformBlockMapSet.Instance.M22[index] ?? 1f
            );
            WorldTransformBackingStore.Instance.M23.Set(
                index, WorldTransformBlockMapSet.Instance.M23[index] ?? 0f
            );
            WorldTransformBackingStore.Instance.M31.Set(
                index, WorldTransformBlockMapSet.Instance.M31[index] ?? 0f
            );
            WorldTransformBackingStore.Instance.M32.Set(
                index, WorldTransformBlockMapSet.Instance.M32[index] ?? 0f
            );
            WorldTransformBackingStore.Instance.M33.Set(
                index, WorldTransformBlockMapSet.Instance.M33[index] ?? 1f
            );
            WorldTransformBackingStore.Instance.M41.Set(
                index, WorldTransformBlockMapSet.Instance.M41[index] ?? 0f
            );
            WorldTransformBackingStore.Instance.M42.Set(
                index, WorldTransformBlockMapSet.Instance.M42[index] ?? 0f
            );
            WorldTransformBackingStore.Instance.M43.Set(
                index, WorldTransformBlockMapSet.Instance.M43[index] ?? 0f
            );
        }
    }

    private void ScatterToChildren(List<ushort> entities)
    {
        foreach (var parentIdx in entities)
        {
            var parent = EntityRegistry.Instance[parentIdx];
            foreach (var child in parent.GetChildren())
            {
                var childIdx = child.Index;

                // Copy only the 12 computed elements
                ParentWorldTransformBackingStore.Instance.M11.Set(
                    childIdx, WorldTransformBackingStore.Instance.M11[parentIdx] ?? 1f
                );
                ParentWorldTransformBackingStore.Instance.M12.Set(
                    childIdx, WorldTransformBackingStore.Instance.M12[parentIdx] ?? 0f
                );
                ParentWorldTransformBackingStore.Instance.M13.Set(
                    childIdx, WorldTransformBackingStore.Instance.M13[parentIdx] ?? 0f
                );
                ParentWorldTransformBackingStore.Instance.M21.Set(
                    childIdx, WorldTransformBackingStore.Instance.M21[parentIdx] ?? 0f
                );
                ParentWorldTransformBackingStore.Instance.M22.Set(
                    childIdx, WorldTransformBackingStore.Instance.M22[parentIdx] ?? 1f
                );
                ParentWorldTransformBackingStore.Instance.M23.Set(
                    childIdx, WorldTransformBackingStore.Instance.M23[parentIdx] ?? 0f
                );
                ParentWorldTransformBackingStore.Instance.M31.Set(
                    childIdx, WorldTransformBackingStore.Instance.M31[parentIdx] ?? 0f
                );
                ParentWorldTransformBackingStore.Instance.M32.Set(
                    childIdx, WorldTransformBackingStore.Instance.M32[parentIdx] ?? 0f
                );
                ParentWorldTransformBackingStore.Instance.M33.Set(
                    childIdx, WorldTransformBackingStore.Instance.M33[parentIdx] ?? 1f
                );
                ParentWorldTransformBackingStore.Instance.M41.Set(
                    childIdx, WorldTransformBackingStore.Instance.M41[parentIdx] ?? 0f
                );
                ParentWorldTransformBackingStore.Instance.M42.Set(
                    childIdx, WorldTransformBackingStore.Instance.M42[parentIdx] ?? 0f
                );
                ParentWorldTransformBackingStore.Instance.M43.Set(
                    childIdx, WorldTransformBackingStore.Instance.M43[parentIdx] ?? 0f
                );

                _dirty.Set(childIdx, true);
            }
        }
    }

    private void FireBulkEvents()
    {
        foreach (var index in _worldTransformUpdated)
        {
            var entity = EntityRegistry.Instance[index];
            EventBus<PostBehaviorUpdatedEvent<WorldTransformBehavior>>.Push(new(entity));
        }
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
        WorldTransformBackingStore.Instance.SetupForEntity(e.Entity);
        e.Entity.SetRefBehavior((ref readonly WorldTransformBehavior _) => { });
        
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
        WorldTransformBackingStore.Instance.CleanupForEntity(entity);
        _worldTransformUpdated.Remove(entity.Index);
    }

    public void OnEvent(BehaviorAddedEvent<Parent> e) => MarkDirty(e.Entity);
    public void OnEvent(PostBehaviorUpdatedEvent<Parent> e) => MarkDirty(e.Entity);
    public void OnEvent(PreBehaviorRemovedEvent<Parent> e) => MarkDirty(e.Entity);
}

