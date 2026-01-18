using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Core.Extensions;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// SIMD-friendly backing storage for all transform inputs.
/// </summary>
public sealed class TransformStore : 
    ISingleton<TransformStore>,
    IEventHandler<InitializeEvent>,
    IEventHandler<PostEntityDeactivatedEvent>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
        EventBus<InitializeEvent>.Register<TransformStore>();
    }

    public static TransformStore Instance { get; private set; } = null!;

    // Position (default: 0, 0, 0)
    public float[] PositionX = new float[Constants.MaxEntities];
    public float[] PositionY = new float[Constants.MaxEntities];
    public float[] PositionZ = new float[Constants.MaxEntities];

    // Rotation quaternion (default: identity = 0, 0, 0, 1)
    public float[] RotationX = new float[Constants.MaxEntities];
    public float[] RotationY = new float[Constants.MaxEntities];
    public float[] RotationZ = new float[Constants.MaxEntities];
    public float[] RotationW = [.. Enumerable.Repeat(1f, Constants.MaxEntities)];

    // Scale (default: 1, 1, 1)
    public float[] ScaleX = [.. Enumerable.Repeat(1f, Constants.MaxEntities)];
    public float[] ScaleY = [.. Enumerable.Repeat(1f, Constants.MaxEntities)];
    public float[] ScaleZ = [.. Enumerable.Repeat(1f, Constants.MaxEntities)];

    // Anchor (default: 0, 0, 0)
    public float[] AnchorX = new float[Constants.MaxEntities];
    public float[] AnchorY = new float[Constants.MaxEntities];
    public float[] AnchorZ = new float[Constants.MaxEntities];

    public TransformBehavior CreateFor(Entity entity)
    {
        var idx = entity.Index;
        return new TransformBehavior(
            Position: new BackedVector3(
                PositionX.BackedFor(idx),
                PositionY.BackedFor(idx),
                PositionZ.BackedFor(idx)
            ),
            Rotation: new BackedQuaternion(
                RotationX.BackedFor(idx),
                RotationY.BackedFor(idx),
                RotationZ.BackedFor(idx),
                RotationW.BackedFor(idx)
            ),
            Scale: new BackedVector3(
                ScaleX.BackedFor(idx),
                ScaleY.BackedFor(idx),
                ScaleZ.BackedFor(idx)
            ),
            Anchor: new BackedVector3(
                AnchorX.BackedFor(idx),
                AnchorY.BackedFor(idx),
                AnchorZ.BackedFor(idx)
            )
        );
    }

    public void OnEvent(InitializeEvent _)
    {
        EventBus<PostEntityDeactivatedEvent>.Register<TransformStore>();
    }

    public void OnEvent(PostEntityDeactivatedEvent e)
    {
        PositionX[e.Entity.Index] = 0f;
        PositionY[e.Entity.Index] = 0f;
        PositionZ[e.Entity.Index] = 0f;
        
        RotationX[e.Entity.Index] = 0f;
        RotationY[e.Entity.Index] = 0f;
        RotationZ[e.Entity.Index] = 0f;
        RotationW[e.Entity.Index] = 1f;
        
        ScaleX[e.Entity.Index] = 1f;
        ScaleY[e.Entity.Index] = 1f;
        ScaleZ[e.Entity.Index] = 1f;

        AnchorX[e.Entity.Index] = 0f;
        AnchorY[e.Entity.Index] = 0f;
        AnchorZ[e.Entity.Index] = 0f;
    }
}
