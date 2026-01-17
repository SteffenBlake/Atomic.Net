using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Core.BlockMaps;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// SIMD-friendly backing storage for all transform inputs.
/// </summary>
public sealed class TransformBackingStore : ISingleton<TransformBackingStore>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
    }

    public static TransformBackingStore Instance { get; private set; } = null!;

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

    /// <summary>
    /// Initializes transform backing store entries for an entity with default values.
    /// </summary>
    public void SetupForEntity(Entity entity)
    {
        var idx = entity.Index;
        PositionX.Set(idx, 0f);
        PositionY.Set(idx, 0f);
        PositionZ.Set(idx, 0f);
        RotationX.Set(idx, 0f);
        RotationY.Set(idx, 0f);
        RotationZ.Set(idx, 0f);
        RotationW.Set(idx, 1f);
        ScaleX.Set(idx, 1f);
        ScaleY.Set(idx, 1f);
        ScaleZ.Set(idx, 1f);
        AnchorX.Set(idx, 0f);
        AnchorY.Set(idx, 0f);
        AnchorZ.Set(idx, 0f);
    }

    /// <summary>
    /// Clears transform backing store entries for an entity, resetting to default values.
    /// </summary>
    public void CleanupForEntity(Entity entity)
    {
        SetupForEntity(entity);
    }
}
