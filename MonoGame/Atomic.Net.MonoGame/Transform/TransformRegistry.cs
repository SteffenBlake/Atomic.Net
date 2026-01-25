using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Microsoft.Xna.Framework;
using Atomic.Net.MonoGame.Hierarchy;

namespace Atomic.Net.MonoGame.Transform;

public sealed class TransformRegistry :
    ISingleton<TransformRegistry>,
    IEventHandler<InitializeEvent>,
    IEventHandler<BehaviorAddedEvent<TransformBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<TransformBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<TransformBehavior>>,
    IEventHandler<BehaviorAddedEvent<ParentBehavior>>,
    IEventHandler<PostBehaviorUpdatedEvent<ParentBehavior>>,
    IEventHandler<PreBehaviorRemovedEvent<ParentBehavior>>
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

    public void OnEvent(InitializeEvent _)
    {
        EventBus<BehaviorAddedEvent<TransformBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<TransformBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<TransformBehavior>>.Register(this);

        EventBus<BehaviorAddedEvent<ParentBehavior>>.Register(this);
        EventBus<PostBehaviorUpdatedEvent<ParentBehavior>>.Register(this);
        EventBus<PreBehaviorRemovedEvent<ParentBehavior>>.Register(this);
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

                // Skip if any ancestor is dirty on FIRST iteration only - they will mark us dirty again after they process
                if (iterations == 1 && HasDirtyAncestor(entity))
                {
                    continue;
                }

                if (!BehaviorRegistry<TransformBehavior>.Instance.TryGetBehavior(entity, out var _transform))
                {
                    continue;
                }

                var anchor = _transform.Value.Anchor;
                var scale = _transform.Value.Scale;
                var rotation = _transform.Value.Rotation;
                var position = _transform.Value.Position;

                Vector3.Multiply(ref anchor, -1, out _localAnchorNegV);

                Matrix.CreateTranslation(ref _localAnchorNegV, out _localAnchorNeg);
                Matrix.CreateScale(ref scale, out _localScale);
                Matrix.CreateFromQuaternion(ref rotation, out _localRotation);
                Matrix.CreateTranslation(ref anchor, out _localAnchor);
                Matrix.CreateTranslation(ref position, out _localpos);

                // ========================================================================
                // DISCOVERY: Transform Matrix Multiplication Order (Sprint 001 - Jan 2026)
                // ========================================================================
                // 
                // This matrix order is CORRECT for MonoGame/XNA hierarchical transforms:
                //   (-Anchor) * Scale * Rotation * Anchor * Position
                //
                // When anchor is zero (default), this simplifies to:
                //   Scale * Rotation * Position
                //
                // WHY THIS ORDER MATTERS:
                // For a parent at position (100,0,0) rotated 90° with child at local (50,0,0):
                //   ✓ CORRECT: Rotate child (50,0,0) → (0,50,0), add parent pos → (100,50,0)
                //   ✗ WRONG:   Translate first then rotate → nonsensical positioning
                //
                // CRITICAL LESSON: When position is zero, Rotation*Translation(0,0,0) equals
                // Translation(0,0,0)*Rotation (both are just the rotation matrix). This can
                // hide matrix order bugs! Always test with NON-ZERO values.
                //
                // INVESTIGATION: During Sprint 001, integration tests appeared to fail due to
                // "matrix order bug". Investigation revealed the Transform system was CORRECT.
                // The test expectations were wrong (they expected Position*Rotation instead of
                // Rotation*Position). Fixed by correcting 2 test expectations, no production
                // code changes needed.
                //
                // See: /TRANSFORM_TEST_INVESTIGATION.md for detailed analysis
                // See: .github/agents/DISCOVERIES.md for discovery entry
                // ========================================================================
                Matrix.Multiply(ref _localAnchorNeg, ref _localScale, out _localTransform);
                Matrix.Multiply(ref _localTransform, ref _localRotation, out _localTransform);
                Matrix.Multiply(ref _localTransform, ref _localAnchor, out _localTransform);
                Matrix.Multiply(ref _localTransform, ref _localpos, out _localTransform);

                // Get parent world transform
                Matrix parentWorldTransform = Matrix.Identity;
                if (entity.TryGetParent(out var _parent))
                {
                    if (_parent.Value.TryGetBehavior<WorldTransformBehavior>(out var _parentWorld))
                    {
                        parentWorldTransform = _parentWorld.Value.Value;
                    }
                }

                var worldTransform = _localTransform * parentWorldTransform;
                // Update world transform behavior
                entity.SetBehavior<WorldTransformBehavior, Matrix>(
                    in worldTransform,
                    (ref readonly input, ref wt) => wt.Value = input
                );

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

    private bool HasDirtyAncestor(Entity entity)
    {
        var current = entity;
        while (current.TryGetParent(out var _parent))
        {
            if (_dirty.HasValue(_parent.Value.Index))
            {
                return true;
            }
            current = _parent.Value;
        }
        return false;
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

    public void OnEvent(BehaviorAddedEvent<ParentBehavior> e) => MarkDirty(e.Entity);
    public void OnEvent(PostBehaviorUpdatedEvent<ParentBehavior> e) => MarkDirty(e.Entity);
    public void OnEvent(PreBehaviorRemovedEvent<ParentBehavior> e) => MarkDirty(e.Entity);
}
