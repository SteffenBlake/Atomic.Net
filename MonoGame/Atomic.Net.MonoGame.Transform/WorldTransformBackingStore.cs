using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Core.BlockMaps;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Backing store for world transform behavior data.
/// </summary>
public static class WorldTransformBackingStore
{
    public static BlockMapBase M11 => WorldTransformBlockMapSet.Instance.M11;
    public static BlockMapBase M12 => WorldTransformBlockMapSet.Instance.M12;
    public static BlockMapBase M13 => WorldTransformBlockMapSet.Instance.M13;
    
    public static BlockMapBase M21 => WorldTransformBlockMapSet.Instance.M21;
    public static BlockMapBase M22 => WorldTransformBlockMapSet.Instance.M22;
    public static BlockMapBase M23 => WorldTransformBlockMapSet.Instance.M23;
    
    public static BlockMapBase M31 => WorldTransformBlockMapSet.Instance.M31;
    public static BlockMapBase M32 => WorldTransformBlockMapSet.Instance.M32;
    public static BlockMapBase M33 => WorldTransformBlockMapSet.Instance.M33;
    
    public static BlockMapBase M41 => WorldTransformBlockMapSet.Instance.M41;
    public static BlockMapBase M42 => WorldTransformBlockMapSet.Instance.M42;
    public static BlockMapBase M43 => WorldTransformBlockMapSet.Instance.M43;

    // M14, M24, M34, M44 are NOT computed - they are always (0, 0, 0, 1) for affine transforms
    public static InputBlockMap M14 { get; } = new(initValue: 0f, dense: true);
    public static InputBlockMap M24 { get; } = new(initValue: 0f, dense: true);
    public static InputBlockMap M34 { get; } = new(initValue: 0f, dense: true);
    public static InputBlockMap M44 { get; } = new(initValue: 1f, dense: true);

    /// <summary>
    /// Creates a WorldTransform for the specified entity.
    /// </summary>
    public static WorldTransformBehavior CreateFor(Entity entity) => new(
        new ReadOnlyBackedMatrix(
            M11.ReadOnlyInstanceFor(entity.Index),
            M12.ReadOnlyInstanceFor(entity.Index),
            M13.ReadOnlyInstanceFor(entity.Index),
            M14.ReadOnlyInstanceFor(entity.Index),
            M21.ReadOnlyInstanceFor(entity.Index),
            M22.ReadOnlyInstanceFor(entity.Index),
            M23.ReadOnlyInstanceFor(entity.Index),
            M24.ReadOnlyInstanceFor(entity.Index),
            M31.ReadOnlyInstanceFor(entity.Index),
            M32.ReadOnlyInstanceFor(entity.Index),
            M33.ReadOnlyInstanceFor(entity.Index),
            M34.ReadOnlyInstanceFor(entity.Index),
            M41.ReadOnlyInstanceFor(entity.Index),
            M42.ReadOnlyInstanceFor(entity.Index),
            M43.ReadOnlyInstanceFor(entity.Index),
            M44.ReadOnlyInstanceFor(entity.Index)
        )
    );
}
