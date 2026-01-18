using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.Hierarchy;
using System.Numerics.Tensors;

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

    // LocalTransform matrix buffers (12 total - each used 3x in WorldTransform calculation)
    private readonly float[] _localTransformM11 = new float[Constants.MaxEntities];
    private readonly float[] _localTransformM12 = new float[Constants.MaxEntities];
    private readonly float[] _localTransformM13 = new float[Constants.MaxEntities];
    private readonly float[] _localTransformM21 = new float[Constants.MaxEntities];
    private readonly float[] _localTransformM22 = new float[Constants.MaxEntities];
    private readonly float[] _localTransformM23 = new float[Constants.MaxEntities];
    private readonly float[] _localTransformM31 = new float[Constants.MaxEntities];
    private readonly float[] _localTransformM32 = new float[Constants.MaxEntities];
    private readonly float[] _localTransformM33 = new float[Constants.MaxEntities];
    private readonly float[] _localTransformM41 = new float[Constants.MaxEntities];
    private readonly float[] _localTransformM42 = new float[Constants.MaxEntities];
    private readonly float[] _localTransformM43 = new float[Constants.MaxEntities];

    // Quaternion product cache buffers (9 total - eliminates redundant calculations)
    private readonly float[] _quatProductXX = new float[Constants.MaxEntities];
    private readonly float[] _quatProductYY = new float[Constants.MaxEntities];
    private readonly float[] _quatProductZZ = new float[Constants.MaxEntities];
    private readonly float[] _quatProductXY = new float[Constants.MaxEntities];
    private readonly float[] _quatProductXZ = new float[Constants.MaxEntities];
    private readonly float[] _quatProductYZ = new float[Constants.MaxEntities];
    private readonly float[] _quatProductWX = new float[Constants.MaxEntities];
    private readonly float[] _quatProductWY = new float[Constants.MaxEntities];
    private readonly float[] _quatProductWZ = new float[Constants.MaxEntities];

    // Ping-pong buffers for intermediate SIMD calculations (5 total)
    private readonly float[] _intermediateBuffer1 = new float[Constants.MaxEntities];
    private readonly float[] _intermediateBuffer2 = new float[Constants.MaxEntities];
    private readonly float[] _intermediateBuffer3 = new float[Constants.MaxEntities];
    private readonly float[] _intermediateBuffer4 = new float[Constants.MaxEntities];
    private readonly float[] _intermediateBuffer5 = new float[Constants.MaxEntities];

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

        // Step 1: Compute LocalTransform from Position/Rotation/Scale/Anchor (ONCE)
        ComputeLocalTransforms();

        // Step 2: Iteratively propagate world transforms through hierarchy
        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            if (_dirty.Count == 0)
            {
                break;
            }

            _dirtyIndices.Clear();
            foreach (var (index, _) in _dirty)
            {
                _dirtyIndices.Add(index);
                _worldTransformUpdated.Set(index, true);
            }
            _dirty.Clear();

            // Compute WorldTransform = LocalTransform × ParentWorldTransform
            ComputeWorldTransforms();

            // Scatter WorldTransform to children's ParentWorldTransform
            ScatterToChildren(_dirtyIndices);
        }

        // Step 3: Fire bulk events
        FireBulkEvents();
    }

    private void ComputeLocalTransforms()
    {
        var _transformStore = TransformStore.Instance;
        
        // Phase 1: Cache all quaternion products to eliminate redundant calculations
        // Each product (xx, yy, zz, xy, xz, yz, wx, wy, wz) is used in multiple rotation matrix elements
        // Caching them once reduces SIMD operations by ~50% in quaternion-to-matrix conversion
        TensorPrimitives.Multiply(
            _transformStore.RotationX, _transformStore.RotationX, _quatProductXX
        );
        TensorPrimitives.Multiply(
            _transformStore.RotationY, _transformStore.RotationY, _quatProductYY
        );
        TensorPrimitives.Multiply(
            _transformStore.RotationZ, _transformStore.RotationZ, _quatProductZZ
        );
        TensorPrimitives.Multiply(
            _transformStore.RotationX, _transformStore.RotationY, _quatProductXY
        );
        TensorPrimitives.Multiply(
            _transformStore.RotationX, _transformStore.RotationZ, _quatProductXZ
        );
        TensorPrimitives.Multiply(
            _transformStore.RotationY, _transformStore.RotationZ, _quatProductYZ
        );
        TensorPrimitives.Multiply(
            _transformStore.RotationW, _transformStore.RotationX, _quatProductWX
        );
        TensorPrimitives.Multiply(
            _transformStore.RotationW, _transformStore.RotationY, _quatProductWY
        );
        TensorPrimitives.Multiply(
            _transformStore.RotationW, _transformStore.RotationZ, _quatProductWZ
        );

        // Phases 2-11: Build rotation matrix and apply transformations using cached products
        // Phase 2: Compute rotation matrix element r11 = 1 - 2(y² + z²), then scale by X
        TensorPrimitives.Multiply(_quatProductYY, 2f, _intermediateBuffer1);
        TensorPrimitives.Multiply(_quatProductZZ, 2f, _intermediateBuffer2);
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer3
        );
        TensorPrimitives.Negate(_intermediateBuffer3, _intermediateBuffer3);
        TensorPrimitives.Add(_intermediateBuffer3, 1f, _intermediateBuffer3);
        TensorPrimitives.Multiply(
            _transformStore.ScaleX, _intermediateBuffer3, _localTransformM11
        );

        // Phase 3: Compute rotation matrix element r21 = 2(xy + wz), then scale by X
        TensorPrimitives.Multiply(_quatProductXY, 2f, _intermediateBuffer1);
        TensorPrimitives.Multiply(_quatProductWZ, 2f, _intermediateBuffer2);
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer3
        );
        TensorPrimitives.Multiply(
            _transformStore.ScaleX, _intermediateBuffer3, _localTransformM12
        );

        // Phase 4: Compute rotation matrix element r31 = 2(xz - wy), then scale by X
        TensorPrimitives.Multiply(_quatProductXZ, 2f, _intermediateBuffer1);
        TensorPrimitives.Multiply(_quatProductWY, 2f, _intermediateBuffer2);
        TensorPrimitives.Subtract(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer3
        );
        TensorPrimitives.Multiply(
            _transformStore.ScaleX, _intermediateBuffer3, _localTransformM13
        );

        // Phase 5: Compute rotation matrix element r12 = 2(xy - wz), then scale by Y
        TensorPrimitives.Multiply(_quatProductXY, 2f, _intermediateBuffer1);
        TensorPrimitives.Multiply(_quatProductWZ, 2f, _intermediateBuffer2);
        TensorPrimitives.Subtract(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer3
        );
        TensorPrimitives.Multiply(
            _transformStore.ScaleY, _intermediateBuffer3, _localTransformM21
        );

        // Phase 6: Compute rotation matrix element r22 = 1 - 2(x² + z²), then scale by Y
        TensorPrimitives.Multiply(_quatProductXX, 2f, _intermediateBuffer1);
        TensorPrimitives.Multiply(_quatProductZZ, 2f, _intermediateBuffer2);
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer3
        );
        TensorPrimitives.Negate(_intermediateBuffer3, _intermediateBuffer3);
        TensorPrimitives.Add(_intermediateBuffer3, 1f, _intermediateBuffer3);
        TensorPrimitives.Multiply(
            _transformStore.ScaleY, _intermediateBuffer3, _localTransformM22
        );

        // Phase 7: Compute rotation matrix element r32 = 2(yz + wx), then scale by Y
        TensorPrimitives.Multiply(_quatProductYZ, 2f, _intermediateBuffer1);
        TensorPrimitives.Multiply(_quatProductWX, 2f, _intermediateBuffer2);
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer3
        );
        TensorPrimitives.Multiply(
            _transformStore.ScaleY, _intermediateBuffer3, _localTransformM23
        );

        // Phase 8: Compute rotation matrix element r13 = 2(xz + wy), then scale by Z
        TensorPrimitives.Multiply(_quatProductXZ, 2f, _intermediateBuffer1);
        TensorPrimitives.Multiply(_quatProductWY, 2f, _intermediateBuffer2);
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer3
        );
        TensorPrimitives.Multiply(
            _transformStore.ScaleZ, _intermediateBuffer3, _localTransformM31
        );

        // Phase 9: Compute rotation matrix element r23 = 2(yz - wx), then scale by Z
        TensorPrimitives.Multiply(_quatProductYZ, 2f, _intermediateBuffer1);
        TensorPrimitives.Multiply(_quatProductWX, 2f, _intermediateBuffer2);
        TensorPrimitives.Subtract(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer3
        );
        TensorPrimitives.Multiply(
            _transformStore.ScaleZ, _intermediateBuffer3, _localTransformM32
        );

        // Phase 10: Compute rotation matrix element r33 = 1 - 2(x² + y²), then scale by Z
        TensorPrimitives.Multiply(_quatProductXX, 2f, _intermediateBuffer1);
        TensorPrimitives.Multiply(_quatProductYY, 2f, _intermediateBuffer2);
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer3
        );
        TensorPrimitives.Negate(_intermediateBuffer3, _intermediateBuffer3);
        TensorPrimitives.Add(_intermediateBuffer3, 1f, _intermediateBuffer3);
        TensorPrimitives.Multiply(
            _transformStore.ScaleZ, _intermediateBuffer3, _localTransformM33
        );

        // Phase 11: Compute translation with anchor transformation
        // transformedAnchorX = M11*anchorX + M21*anchorY + M31*anchorZ
        TensorPrimitives.Multiply(
            _localTransformM11, _transformStore.AnchorX, _intermediateBuffer1
        );
        TensorPrimitives.Multiply(
            _localTransformM21, _transformStore.AnchorY, _intermediateBuffer2
        );
        TensorPrimitives.Multiply(
            _localTransformM31, _transformStore.AnchorZ, _intermediateBuffer3
        );
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _intermediateBuffer4, _intermediateBuffer3, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _transformStore.PositionX, _transformStore.AnchorX, _intermediateBuffer5
        );
        TensorPrimitives.Subtract(
            _intermediateBuffer5, _intermediateBuffer4, _localTransformM41
        );

        // transformedAnchorY = M12*anchorX + M22*anchorY + M32*anchorZ
        TensorPrimitives.Multiply(
            _localTransformM12, _transformStore.AnchorX, _intermediateBuffer1
        );
        TensorPrimitives.Multiply(
            _localTransformM22, _transformStore.AnchorY, _intermediateBuffer2
        );
        TensorPrimitives.Multiply(
            _localTransformM32, _transformStore.AnchorZ, _intermediateBuffer3
        );
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _intermediateBuffer4, _intermediateBuffer3, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _transformStore.PositionY, _transformStore.AnchorY, _intermediateBuffer5
        );
        TensorPrimitives.Subtract(
            _intermediateBuffer5, _intermediateBuffer4, _localTransformM42
        );

        // transformedAnchorZ = M13*anchorX + M23*anchorY + M33*anchorZ
        TensorPrimitives.Multiply(
            _localTransformM13, _transformStore.AnchorX, _intermediateBuffer1
        );
        TensorPrimitives.Multiply(
            _localTransformM23, _transformStore.AnchorY, _intermediateBuffer2
        );
        TensorPrimitives.Multiply(
            _localTransformM33, _transformStore.AnchorZ, _intermediateBuffer3
        );
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _intermediateBuffer4, _intermediateBuffer3, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _transformStore.PositionZ, _transformStore.AnchorZ, _intermediateBuffer5
        );
        TensorPrimitives.Subtract(
            _intermediateBuffer5, _intermediateBuffer4, _localTransformM43
        );
    }

    private void ComputeWorldTransforms()
    {
        var parentWorldTransformStoreIn = ParentWorldTransformStore.Instance;
        var worldTransformStoreOut = WorldTransformStore.Instance;
        // WorldTransform M11 = LocalTransform[M11*PWT_M11 + M12*PWT_M21 + M13*PWT_M31]
        TensorPrimitives.Multiply(
            _localTransformM11, parentWorldTransformStoreIn.M11, _intermediateBuffer1
        );
        TensorPrimitives.Multiply(
            _localTransformM12, parentWorldTransformStoreIn.M21, _intermediateBuffer2
        );
        TensorPrimitives.Multiply(
            _localTransformM13, parentWorldTransformStoreIn.M31, _intermediateBuffer3
        );
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _intermediateBuffer4, _intermediateBuffer3, worldTransformStoreOut.M11
        );

        // WorldTransform M12 = LocalTransform[M11*PWT_M12 + M12*PWT_M22 + M13*PWT_M32]
        TensorPrimitives.Multiply(
            _localTransformM11, parentWorldTransformStoreIn.M12, _intermediateBuffer1
        );
        TensorPrimitives.Multiply(
            _localTransformM12, parentWorldTransformStoreIn.M22, _intermediateBuffer2
        );
        TensorPrimitives.Multiply(
            _localTransformM13, parentWorldTransformStoreIn.M32, _intermediateBuffer3
        );
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _intermediateBuffer4, _intermediateBuffer3, worldTransformStoreOut.M12
        );

        // WorldTransform M13 = LocalTransform[M11*PWT_M13 + M12*PWT_M23 + M13*PWT_M33]
        TensorPrimitives.Multiply(
            _localTransformM11, parentWorldTransformStoreIn.M13, _intermediateBuffer1
        );
        TensorPrimitives.Multiply(
            _localTransformM12, parentWorldTransformStoreIn.M23, _intermediateBuffer2
        );
        TensorPrimitives.Multiply(
            _localTransformM13, parentWorldTransformStoreIn.M33, _intermediateBuffer3
        );
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _intermediateBuffer4, _intermediateBuffer3, worldTransformStoreOut.M13
        );

        // WorldTransform M21 = LocalTransform[M21*PWT_M11 + M22*PWT_M21 + M23*PWT_M31]
        TensorPrimitives.Multiply(
            _localTransformM21, parentWorldTransformStoreIn.M11, _intermediateBuffer1
        );
        TensorPrimitives.Multiply(
            _localTransformM22, parentWorldTransformStoreIn.M21, _intermediateBuffer2
        );
        TensorPrimitives.Multiply(
            _localTransformM23, parentWorldTransformStoreIn.M31, _intermediateBuffer3
        );
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _intermediateBuffer4, _intermediateBuffer3, worldTransformStoreOut.M21
        );

        // WorldTransform M22 = LocalTransform[M21*PWT_M12 + M22*PWT_M22 + M23*PWT_M32]
        TensorPrimitives.Multiply(
            _localTransformM21, parentWorldTransformStoreIn.M12, _intermediateBuffer1
        );
        TensorPrimitives.Multiply(
            _localTransformM22, parentWorldTransformStoreIn.M22, _intermediateBuffer2
        );
        TensorPrimitives.Multiply(
            _localTransformM23, parentWorldTransformStoreIn.M32, _intermediateBuffer3
        );
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _intermediateBuffer4, _intermediateBuffer3, worldTransformStoreOut.M22
        );

        // WorldTransform M23 = LocalTransform[M21*PWT_M13 + M22*PWT_M23 + M23*PWT_M33]
        TensorPrimitives.Multiply(
            _localTransformM21, parentWorldTransformStoreIn.M13, _intermediateBuffer1
        );
        TensorPrimitives.Multiply(
            _localTransformM22, parentWorldTransformStoreIn.M23, _intermediateBuffer2
        );
        TensorPrimitives.Multiply(
            _localTransformM23, parentWorldTransformStoreIn.M33, _intermediateBuffer3
        );
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _intermediateBuffer4, _intermediateBuffer3, worldTransformStoreOut.M23
        );

        // WorldTransform M31 = LocalTransform[M31*PWT_M11 + M32*PWT_M21 + M33*PWT_M31]
        TensorPrimitives.Multiply(
            _localTransformM31, parentWorldTransformStoreIn.M11, _intermediateBuffer1
        );
        TensorPrimitives.Multiply(
            _localTransformM32, parentWorldTransformStoreIn.M21, _intermediateBuffer2
        );
        TensorPrimitives.Multiply(
            _localTransformM33, parentWorldTransformStoreIn.M31, _intermediateBuffer3
        );
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _intermediateBuffer4, _intermediateBuffer3, worldTransformStoreOut.M31
        );

        // WorldTransform M32 = LocalTransform[M31*PWT_M12 + M32*PWT_M22 + M33*PWT_M32]
        TensorPrimitives.Multiply(
            _localTransformM31, parentWorldTransformStoreIn.M12, _intermediateBuffer1
        );
        TensorPrimitives.Multiply(
            _localTransformM32, parentWorldTransformStoreIn.M22, _intermediateBuffer2
        );
        TensorPrimitives.Multiply(
            _localTransformM33, parentWorldTransformStoreIn.M32, _intermediateBuffer3
        );
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _intermediateBuffer4, _intermediateBuffer3, worldTransformStoreOut.M32
        );

        // WorldTransform M33 = LocalTransform[M31*PWT_M13 + M32*PWT_M23 + M33*PWT_M33]
        TensorPrimitives.Multiply(
            _localTransformM31, parentWorldTransformStoreIn.M13, _intermediateBuffer1
        );
        TensorPrimitives.Multiply(
            _localTransformM32, parentWorldTransformStoreIn.M23, _intermediateBuffer2
        );
        TensorPrimitives.Multiply(
            _localTransformM33, parentWorldTransformStoreIn.M33, _intermediateBuffer3
        );
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _intermediateBuffer4, _intermediateBuffer3, worldTransformStoreOut.M33
        );

        // WorldTransform M41 = LocalTransform[M41*PWT_M11 + M42*PWT_M21 + M43*PWT_M31 + PWT_M41]
        TensorPrimitives.Multiply(
            _localTransformM41, parentWorldTransformStoreIn.M11, _intermediateBuffer1
        );
        TensorPrimitives.Multiply(
            _localTransformM42, parentWorldTransformStoreIn.M21, _intermediateBuffer2
        );
        TensorPrimitives.Multiply(
            _localTransformM43, parentWorldTransformStoreIn.M31, _intermediateBuffer3
        );
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _intermediateBuffer4, _intermediateBuffer3, _intermediateBuffer5
        );
        TensorPrimitives.Add(
            _intermediateBuffer5, parentWorldTransformStoreIn.M41, worldTransformStoreOut.M41
        );

        // WorldTransform M42 = LocalTransform[M41*PWT_M12 + M42*PWT_M22 + M43*PWT_M32 + PWT_M42]
        TensorPrimitives.Multiply(
            _localTransformM41, parentWorldTransformStoreIn.M12, _intermediateBuffer1
        );
        TensorPrimitives.Multiply(
            _localTransformM42, parentWorldTransformStoreIn.M22, _intermediateBuffer2
        );
        TensorPrimitives.Multiply(
            _localTransformM43, parentWorldTransformStoreIn.M32, _intermediateBuffer3
        );
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _intermediateBuffer4, _intermediateBuffer3, _intermediateBuffer5
        );
        TensorPrimitives.Add(
            _intermediateBuffer5, parentWorldTransformStoreIn.M42, worldTransformStoreOut.M42
        );

        // WorldTransform M43 = LocalTransform[M41*PWT_M13 + M42*PWT_M23 + M43*PWT_M33 + PWT_M43]
        TensorPrimitives.Multiply(
            _localTransformM41, parentWorldTransformStoreIn.M13, _intermediateBuffer1
        );
        TensorPrimitives.Multiply(
            _localTransformM42, parentWorldTransformStoreIn.M23, _intermediateBuffer2
        );
        TensorPrimitives.Multiply(
            _localTransformM43, parentWorldTransformStoreIn.M33, _intermediateBuffer3
        );
        TensorPrimitives.Add(
            _intermediateBuffer1, _intermediateBuffer2, _intermediateBuffer4
        );
        TensorPrimitives.Add(
            _intermediateBuffer4, _intermediateBuffer3, _intermediateBuffer5
        );
        TensorPrimitives.Add(
            _intermediateBuffer5, parentWorldTransformStoreIn.M43, worldTransformStoreOut.M43
        );
    }

    private void ScatterToChildren(List<ushort> parentIndices)
    {
        var worldTransformStore = WorldTransformStore.Instance;
        var parentWorldTransformStore = ParentWorldTransformStore.Instance;

        foreach (var parentIndex in parentIndices)
        {
            var children = HierarchyRegistry.Instance.GetChildrenArray(parentIndex);
            if (children == null)
            {
                continue;
            }

            // Cache parent world transform values (12 reads)
            float parentM11 = worldTransformStore.M11[parentIndex];
            float parentM12 = worldTransformStore.M12[parentIndex];
            float parentM13 = worldTransformStore.M13[parentIndex];
            float parentM21 = worldTransformStore.M21[parentIndex];
            float parentM22 = worldTransformStore.M22[parentIndex];
            float parentM23 = worldTransformStore.M23[parentIndex];
            float parentM31 = worldTransformStore.M31[parentIndex];
            float parentM32 = worldTransformStore.M32[parentIndex];
            float parentM33 = worldTransformStore.M33[parentIndex];
            float parentM41 = worldTransformStore.M41[parentIndex];
            float parentM42 = worldTransformStore.M42[parentIndex];
            float parentM43 = worldTransformStore.M43[parentIndex];

            foreach (var (childIndex, _) in children)
            {
                // Direct scatter to child's parent transform (12 writes)
                parentWorldTransformStore.M11[childIndex] = parentM11;
                parentWorldTransformStore.M12[childIndex] = parentM12;
                parentWorldTransformStore.M13[childIndex] = parentM13;
                parentWorldTransformStore.M21[childIndex] = parentM21;
                parentWorldTransformStore.M22[childIndex] = parentM22;
                parentWorldTransformStore.M23[childIndex] = parentM23;
                parentWorldTransformStore.M31[childIndex] = parentM31;
                parentWorldTransformStore.M32[childIndex] = parentM32;
                parentWorldTransformStore.M33[childIndex] = parentM33;
                parentWorldTransformStore.M41[childIndex] = parentM41;
                parentWorldTransformStore.M42[childIndex] = parentM42;
                parentWorldTransformStore.M43[childIndex] = parentM43;

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
        e.Entity.SetRefBehavior<WorldTransformBehavior>();
        MarkDirty(e.Entity);
    }

    public void OnEvent(PostBehaviorUpdatedEvent<TransformBehavior> e) => MarkDirty(e.Entity);

    public void OnEvent(PreBehaviorRemovedEvent<TransformBehavior> e)
    {
        _dirty.Remove(e.Entity.Index);
        _worldTransformUpdated.Remove(e.Entity.Index);
        MarkDirty(e.Entity);
    }

    public void OnEvent(BehaviorAddedEvent<Parent> e) => MarkDirty(e.Entity);
    public void OnEvent(PostBehaviorUpdatedEvent<Parent> e) => MarkDirty(e.Entity);
    public void OnEvent(PreBehaviorRemovedEvent<Parent> e) => MarkDirty(e.Entity);
}
