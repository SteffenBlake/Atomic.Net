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

    /// <summary>
    /// Creates a BackedMatrix for the specified entity index.
    /// </summary>
    public BackedMatrix InstanceFor(int entityIndex) => new(
        M11.InstanceFor(entityIndex), M12.InstanceFor(entityIndex), M13.InstanceFor(entityIndex), M14.InstanceFor(entityIndex),
        M21.InstanceFor(entityIndex), M22.InstanceFor(entityIndex), M23.InstanceFor(entityIndex), M24.InstanceFor(entityIndex),
        M31.InstanceFor(entityIndex), M32.InstanceFor(entityIndex), M33.InstanceFor(entityIndex), M34.InstanceFor(entityIndex),
        M41.InstanceFor(entityIndex), M42.InstanceFor(entityIndex), M43.InstanceFor(entityIndex), M44.InstanceFor(entityIndex)
    );
}
