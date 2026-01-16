using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.BED.BlockMaps;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Backing store for world transform behavior data.
/// </summary>
public sealed class WorldTransformBackingStore : ISingleton<WorldTransformBackingStore>
{
    public static WorldTransformBackingStore Instance { get; } = new();

    public InputBlockMap M11 { get; } = new();
    public InputBlockMap M12 { get; } = new();
    public InputBlockMap M13 { get; } = new();
    public InputBlockMap M14 { get; } = new();
    
    public InputBlockMap M21 { get; } = new();
    public InputBlockMap M22 { get; } = new();
    public InputBlockMap M23 { get; } = new();
    public InputBlockMap M24 { get; } = new();
    
    public InputBlockMap M31 { get; } = new();
    public InputBlockMap M32 { get; } = new();
    public InputBlockMap M33 { get; } = new();
    public InputBlockMap M34 { get; } = new();
    
    public InputBlockMap M41 { get; } = new();
    public InputBlockMap M42 { get; } = new();
    public InputBlockMap M43 { get; } = new();
    public InputBlockMap M44 { get; } = new();
}
