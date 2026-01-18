using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Core.Extensions;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Backing store for world transform behavior data.
/// </summary>
public class WorldTransformStore : 
    ISingleton<WorldTransformStore>,
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
        EventBus<InitializeEvent>.Register(Instance);
    }

    public static WorldTransformStore Instance { get; private set; } = null!;

    public readonly float[] M11 = [.. Enumerable.Repeat(1f, Constants.MaxEntities)];
    public readonly float[] M12 = new float[Constants.MaxEntities];
    public readonly float[] M13 = new float[Constants.MaxEntities];
    public readonly float[] M14 = new float[Constants.MaxEntities];

    public readonly float[] M21 = new float[Constants.MaxEntities];
    public readonly float[] M22 = [.. Enumerable.Repeat(1f, Constants.MaxEntities)];
    public readonly float[] M23 = new float[Constants.MaxEntities];
    public readonly float[] M24 = new float[Constants.MaxEntities];

    public readonly float[] M31 = new float[Constants.MaxEntities];
    public readonly float[] M32 = new float[Constants.MaxEntities];
    public readonly float[] M33 = [.. Enumerable.Repeat(1f, Constants.MaxEntities)];
    public readonly float[] M34 = new float[Constants.MaxEntities];

    public readonly float[] M41 = new float[Constants.MaxEntities];
    public readonly float[] M42 = new float[Constants.MaxEntities];
    public readonly float[] M43 = new float[Constants.MaxEntities];
    public readonly float[] M44 = [.. Enumerable.Repeat(1f, Constants.MaxEntities)];

    /// <summary>
    /// Creates a WorldTransform for the specified entity.
    /// </summary>
    public WorldTransformBehavior CreateFor(Entity entity) => new(
        new ReadOnlyBackedMatrix(
            M11.ReadOnlyBackedFor(entity.Index),
            M12.ReadOnlyBackedFor(entity.Index),
            M13.ReadOnlyBackedFor(entity.Index),
            M14.ReadOnlyBackedFor(entity.Index),
            M21.ReadOnlyBackedFor(entity.Index),
            M22.ReadOnlyBackedFor(entity.Index),
            M23.ReadOnlyBackedFor(entity.Index),
            M24.ReadOnlyBackedFor(entity.Index),
            M31.ReadOnlyBackedFor(entity.Index),
            M32.ReadOnlyBackedFor(entity.Index),
            M33.ReadOnlyBackedFor(entity.Index),
            M34.ReadOnlyBackedFor(entity.Index),
            M41.ReadOnlyBackedFor(entity.Index),
            M42.ReadOnlyBackedFor(entity.Index),
            M43.ReadOnlyBackedFor(entity.Index),
            M44.ReadOnlyBackedFor(entity.Index)
        )
    );

    public void OnEvent(InitializeEvent e)
    {
        EventBus<PostEntityDeactivatedEvent>.Register<WorldTransformStore>();
    }

    public void OnEvent(PostEntityDeactivatedEvent e)
    {
        M11[e.Entity.Index] = 1f;
        M12[e.Entity.Index] = 0f;
        M13[e.Entity.Index] = 0f;
        M14[e.Entity.Index] = 0f;

        M21[e.Entity.Index] = 0f;
        M22[e.Entity.Index] = 1f;
        M23[e.Entity.Index] = 0f;
        M24[e.Entity.Index] = 0f;
        
        M31[e.Entity.Index] = 0f;
        M32[e.Entity.Index] = 0f;
        M33[e.Entity.Index] = 1f;
        M34[e.Entity.Index] = 0f;

        M41[e.Entity.Index] = 0f;
        M42[e.Entity.Index] = 0f;
        M43[e.Entity.Index] = 0f;
        M44[e.Entity.Index] = 1f;
    }
}
