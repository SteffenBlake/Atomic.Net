using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Core.BlockMaps;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// SIMD-friendly backing storage for cached parent world transforms. Initialized to identity matrix.
/// </summary>
public sealed class ParentWorldTransformBackingStore : ISingleton<ParentWorldTransformBackingStore>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
    }

    public static ParentWorldTransformBackingStore Instance { get; private set; } = null!;

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

    public void EnsureFor(Entity entity)
    {
        SetupForEntity(entity);
    }

    /// <summary>
    /// Initializes parent world transform backing store entries for an entity with identity matrix values.
    /// </summary>
    public void SetupForEntity(Entity entity)
    {
        var idx = entity.Index;
        M11.Set(idx, 1f);
        M12.Set(idx, 0f);
        M13.Set(idx, 0f);
        M21.Set(idx, 0f);
        M22.Set(idx, 1f);
        M23.Set(idx, 0f);
        M31.Set(idx, 0f);
        M32.Set(idx, 0f);
        M33.Set(idx, 1f);
        M41.Set(idx, 0f);
        M42.Set(idx, 0f);
        M43.Set(idx, 0f);
    }

    /// <summary>
    /// Clears parent world transform backing store entries for an entity, resetting to identity matrix.
    /// </summary>
    public void CleanupForEntity(Entity entity)
    {
        SetupForEntity(entity);
    }
}
