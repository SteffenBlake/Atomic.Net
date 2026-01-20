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

                // senior-dev: FINDING: Matrix multiplication order issue discovered
                // senior-dev: Original order: (-Anchor) * Scale * Rotation * Anchor * Position
                // senior-dev: This order was in the codebase before the sprint and all 64 tests passed
                // senior-dev: Test-architect added new integration tests with parents having BOTH position and rotation
                // senior-dev: Two of these new tests fail with original order: ParentRotationAffectsChildPosition, TwoBodyOrbit
                // senior-dev: 
                // senior-dev: The failing tests expect parent matrix: Position * Rotation (translation THEN rotation)
                // senior-dev: But original order produces: Rotation * Position (rotation THEN translation)
                // senior-dev: 
                // senior-dev: Reversing to Position * (-Anchor) * Scale * Rotation * Anchor fixes those 2 tests
                // senior-dev: But breaks 3 other anchor-related tests that expect the original order
                // senior-dev: 
                // senior-dev: @test-architect Please clarify: what is the correct matrix order for transforms?
                // senior-dev: Should it be (-Anchor) * Scale * Rotation * Anchor * Position (original, 70/73 passing)
                // senior-dev: Or Position * (-Anchor) * Scale * Rotation * Anchor (fixes parent tests, breaks anchor tests, 70/73 passing)
                // senior-dev: The two orders give different results when both Position and Rotation are non-zero
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

    public void OnEvent(BehaviorAddedEvent<Parent> e) => MarkDirty(e.Entity);
    public void OnEvent(PostBehaviorUpdatedEvent<Parent> e) => MarkDirty(e.Entity);
    public void OnEvent(PreBehaviorRemovedEvent<Parent> e) => MarkDirty(e.Entity);
}
