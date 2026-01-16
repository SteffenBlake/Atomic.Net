using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Core.BlockMaps;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// SIMD-friendly backing storage for all transform inputs.
/// </summary>
public sealed class TransformBackingStore : ISingleton<TransformBackingStore>
{
    public static TransformBackingStore Instance { get; } = new();

    // Position (default: 0, 0, 0)
    public InputBlockMap PositionX { get; } = new(initValue: 0f);
    public InputBlockMap PositionY { get; } = new(initValue: 0f);
    public InputBlockMap PositionZ { get; } = new(initValue: 0f);

    // Rotation quaternion (default: identity = 0, 0, 0, 1)
    public InputBlockMap RotationX { get; } = new(initValue: 0f);
    public InputBlockMap RotationY { get; } = new(initValue: 0f);
    public InputBlockMap RotationZ { get; } = new(initValue: 0f);
    public InputBlockMap RotationW { get; } = new(initValue: 1f);

    // Scale (default: 1, 1, 1)
    public InputBlockMap ScaleX { get; } = new(initValue: 1f);
    public InputBlockMap ScaleY { get; } = new(initValue: 1f);
    public InputBlockMap ScaleZ { get; } = new(initValue: 1f);

    // Anchor (default: 0, 0, 0)
    public InputBlockMap AnchorX { get; } = new(initValue: 0f);
    public InputBlockMap AnchorY { get; } = new(initValue: 0f);
    public InputBlockMap AnchorZ { get; } = new(initValue: 0f);

    public TransformBehavior CreateFor(Entity entity)
    {
        var idx = entity.Index;
        return new TransformBehavior(
            Position: new BackedVector3(
                PositionX.InstanceFor(idx),
                PositionY.InstanceFor(idx),
                PositionZ.InstanceFor(idx)
            ),
            Rotation: new BackedQuaternion(
                RotationX.InstanceFor(idx),
                RotationY.InstanceFor(idx),
                RotationZ.InstanceFor(idx),
                RotationW.InstanceFor(idx)
            ),
            Scale: new BackedVector3(
                ScaleX.InstanceFor(idx),
                ScaleY.InstanceFor(idx),
                ScaleZ.InstanceFor(idx)
            ),
            Anchor: new BackedVector3(
                AnchorX.InstanceFor(idx),
                AnchorY.InstanceFor(idx),
                AnchorZ.InstanceFor(idx)
            )
        );
    }
}
