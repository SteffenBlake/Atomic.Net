using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.BlockMaps;
using Atomic.Net.MonoGame.BED.Hierarchy;

namespace Atomic.Net.MonoGame.Transform;

public sealed class TransformRegistry : 
    ISingleton<TransformRegistry>,
    IEventHandler<BehaviorAddedEvent<TransformBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<TransformBehavior>>,
    IEventHandler<BehaviorRemovedEvent<TransformBehavior>>,
    IEventHandler<BehaviorAddedEvent<Parent>>,
    IEventHandler<PostBehaviorUpdatedEvent<Parent>>,
    IEventHandler<BehaviorRemovedEvent<Parent>>
{
    public static TransformRegistry Instance { get; } = new();

    private readonly SparseArray<bool> _dirty = new(Constants.MaxEntities);
    private readonly HashSet<ushort> _worldTransformUpdated = new(Constants.MaxEntities);

    private readonly TransformBackingStore _inputStore = TransformBackingStore.Instance;
    private readonly WorldTransformBackingStore _worldStore = WorldTransformBackingStore.Instance;
    private readonly ParentWorldTransformBackingStore _parentStore = ParentWorldTransformBackingStore.Instance;

    private readonly LocalTransformBlockMapSet _localTransformMaps;
    private readonly WorldTransformBlockMapSet _worldTransformMaps;

    private TransformRegistry()
    {
        _localTransformMaps = new LocalTransformBlockMapSet(_inputStore);
        _worldTransformMaps = new WorldTransformBlockMapSet(_localTransformMaps, _parentStore);
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
            _localTransformMaps.Recalculate();

            // Step 2: SIMD compute WorldTransform = LocalTransform × ParentWorldTransform
            _worldTransformMaps.Recalculate();

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
        foreach (var idx in entities)
        {
            // Only copy the 12 computed elements
            // M14, M24, M34, M44 are already preset to (0, 0, 0, 1) via initValue
            _worldStore.M11.Set(idx, _worldTransformMaps.M11[idx] ?? 1f);
            _worldStore.M12.Set(idx, _worldTransformMaps.M12[idx] ?? 0f);
            _worldStore.M13.Set(idx, _worldTransformMaps.M13[idx] ?? 0f);
            _worldStore.M21.Set(idx, _worldTransformMaps.M21[idx] ?? 0f);
            _worldStore.M22.Set(idx, _worldTransformMaps.M22[idx] ?? 1f);
            _worldStore.M23.Set(idx, _worldTransformMaps.M23[idx] ?? 0f);
            _worldStore.M31.Set(idx, _worldTransformMaps.M31[idx] ?? 0f);
            _worldStore.M32.Set(idx, _worldTransformMaps.M32[idx] ?? 0f);
            _worldStore.M33.Set(idx, _worldTransformMaps.M33[idx] ?? 1f);
            _worldStore.M41.Set(idx, _worldTransformMaps.M41[idx] ?? 0f);
            _worldStore.M42.Set(idx, _worldTransformMaps.M42[idx] ?? 0f);
            _worldStore.M43.Set(idx, _worldTransformMaps.M43[idx] ?? 0f);
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
                _parentStore.M11.Set(childIdx, _worldStore.M11[parentIdx] ?? 1f);
                _parentStore.M12.Set(childIdx, _worldStore.M12[parentIdx] ?? 0f);
                _parentStore.M13.Set(childIdx, _worldStore.M13[parentIdx] ?? 0f);
                _parentStore.M21.Set(childIdx, _worldStore.M21[parentIdx] ?? 0f);
                _parentStore.M22.Set(childIdx, _worldStore.M22[parentIdx] ?? 1f);
                _parentStore.M23.Set(childIdx, _worldStore.M23[parentIdx] ?? 0f);
                _parentStore.M31.Set(childIdx, _worldStore.M31[parentIdx] ?? 0f);
                _parentStore.M32.Set(childIdx, _worldStore.M32[parentIdx] ?? 0f);
                _parentStore.M33.Set(childIdx, _worldStore.M33[parentIdx] ?? 1f);
                _parentStore.M41.Set(childIdx, _worldStore.M41[parentIdx] ?? 0f);
                _parentStore.M42.Set(childIdx, _worldStore.M42[parentIdx] ?? 0f);
                _parentStore.M43.Set(childIdx, _worldStore.M43[parentIdx] ?? 0f);

                _dirty.Set(childIdx, true);
            }
        }
    }

    private void FireBulkEvents()
    {
        foreach (var idx in _worldTransformUpdated)
        {
            var entity = EntityRegistry.Instance[idx];
            EventBus<PostBehaviorUpdatedEvent<WorldTransform>>.Push(new(entity));
        }
    }

    private void MarkDirty(Entity entity)
    {
        _dirty.Set(entity.Index, true);
    }

    public void OnEvent(BehaviorAddedEvent<TransformBehavior> e) => MarkDirty(e.Entity);
    public void OnEvent(PostBehaviorUpdatedEvent<TransformBehavior> e) => MarkDirty(e.Entity);
    public void OnEvent(BehaviorRemovedEvent<TransformBehavior> e) => MarkDirty(e.Entity);

    public void OnEvent(BehaviorAddedEvent<Parent> e) => MarkDirty(e.Entity);
    public void OnEvent(PostBehaviorUpdatedEvent<Parent> e) => MarkDirty(e.Entity);
    public void OnEvent(BehaviorRemovedEvent<Parent> e) => MarkDirty(e.Entity);

    public static void Register()
    {
        EventBus<BehaviorAddedEvent<TransformBehavior>>.Register<TransformRegistry>();
        EventBus<PostBehaviorUpdatedEvent<TransformBehavior>>.Register<TransformRegistry>();
        EventBus<BehaviorRemovedEvent<TransformBehavior>>.Register<TransformRegistry>();

        EventBus<BehaviorAddedEvent<Parent>>.Register<TransformRegistry>();
        EventBus<PostBehaviorUpdatedEvent<Parent>>.Register<TransformRegistry>();
        EventBus<BehaviorRemovedEvent<Parent>>.Register<TransformRegistry>();
    }
}

