using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Core.BlockMaps;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Backing store for world transform behavior data.
/// </summary>
public sealed class WorldTransformBackingStore : ISingleton<WorldTransformBackingStore>
{
    public static WorldTransformBackingStore Instance { get; } = new();

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
    public WorldTransform CreateFor(Entity entity) => new(
        new BackedMatrix(
            M11.InstanceFor(entity.Index), M12.InstanceFor(entity.Index), M13.InstanceFor(entity.Index), M14.InstanceFor(entity.Index),
            M21.InstanceFor(entity.Index), M22.InstanceFor(entity.Index), M23.InstanceFor(entity.Index), M24.InstanceFor(entity.Index),
            M31.InstanceFor(entity.Index), M32.InstanceFor(entity.Index), M33.InstanceFor(entity.Index), M34.InstanceFor(entity.Index),
            M41.InstanceFor(entity.Index), M42.InstanceFor(entity.Index), M43.InstanceFor(entity.Index), M44.InstanceFor(entity.Index)
        )
    );
}
