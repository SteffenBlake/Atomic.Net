using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Core.BlockMaps;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Backing store for world transform behavior data.
/// </summary>
public sealed class WorldTransformBackingStore : ISingleton<WorldTransformBackingStore>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
    }

    public static WorldTransformBackingStore Instance { get; private set; } = null!;

    public InputBlockMap M11 { get; } = new(initValue: 1f);
    public InputBlockMap M12 { get; } = new(initValue: 0f);
    public InputBlockMap M13 { get; } = new(initValue: 0f);
    public InputBlockMap M14 { get; } = new(initValue: 0f);
    
    public InputBlockMap M21 { get; } = new(initValue: 0f);
    public InputBlockMap M22 { get; } = new(initValue: 1f);
    public InputBlockMap M23 { get; } = new(initValue: 0f);
    public InputBlockMap M24 { get; } = new(initValue: 0f);
    
    public InputBlockMap M31 { get; } = new(initValue: 0f);
    public InputBlockMap M32 { get; } = new(initValue: 0f);
    public InputBlockMap M33 { get; } = new(initValue: 1f);
    public InputBlockMap M34 { get; } = new(initValue: 0f);
    
    public InputBlockMap M41 { get; } = new(initValue: 0f);
    public InputBlockMap M42 { get; } = new(initValue: 0f);
    public InputBlockMap M43 { get; } = new(initValue: 0f);
    public InputBlockMap M44 { get; } = new(initValue: 1f);

    /// <summary>
    /// Creates a WorldTransform for the specified entity.
    /// </summary>
    public WorldTransformBehavior CreateFor(Entity entity) => new(
        new BackedMatrix(
            M11.InstanceFor(entity.Index),
            M12.InstanceFor(entity.Index),
            M13.InstanceFor(entity.Index),
            M14.InstanceFor(entity.Index),
            M21.InstanceFor(entity.Index),
            M22.InstanceFor(entity.Index),
            M23.InstanceFor(entity.Index),
            M24.InstanceFor(entity.Index),
            M31.InstanceFor(entity.Index),
            M32.InstanceFor(entity.Index),
            M33.InstanceFor(entity.Index),
            M34.InstanceFor(entity.Index),
            M41.InstanceFor(entity.Index),
            M42.InstanceFor(entity.Index),
            M43.InstanceFor(entity.Index),
            M44.InstanceFor(entity.Index)
        )
    );

    /// <summary>
    /// Initializes world transform backing store entries for an entity with identity matrix values.
    /// </summary>
    public void SetupForEntity(Entity entity)
    {
        var idx = entity.Index;
        M11.Set(idx, 1f);
        M12.Set(idx, 0f);
        M13.Set(idx, 0f);
        M14.Set(idx, 0f);
        M21.Set(idx, 0f);
        M22.Set(idx, 1f);
        M23.Set(idx, 0f);
        M24.Set(idx, 0f);
        M31.Set(idx, 0f);
        M32.Set(idx, 0f);
        M33.Set(idx, 1f);
        M34.Set(idx, 0f);
        M41.Set(idx, 0f);
        M42.Set(idx, 0f);
        M43.Set(idx, 0f);
        M44.Set(idx, 1f);
    }

    /// <summary>
    /// Clears world transform backing store entries for an entity, resetting to identity matrix.
    /// </summary>
    public void CleanupForEntity(Entity entity)
    {
        SetupForEntity(entity);
    }
}
