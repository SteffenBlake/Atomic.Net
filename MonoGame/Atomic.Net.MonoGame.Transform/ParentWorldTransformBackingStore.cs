using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core.BlockMaps;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// SIMD-friendly backing storage for cached parent world transforms. Initialized to identity matrix.
/// </summary>
public sealed class ParentWorldTransformBackingStore : ISingleton<ParentWorldTransformBackingStore>
{
    public static ParentWorldTransformBackingStore Instance { get; } = new();

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
}
