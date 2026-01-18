using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Core.Extensions;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Backing store for world transform behavior data.
/// </summary>
public static class WorldTransformStore
{
    public static readonly float[] M11 = [.. Enumerable.Repeat(1f, Constants.MaxEntities)];
    public static readonly float[] M12 = new float[Constants.MaxEntities];
    public static readonly float[] M13 = new float[Constants.MaxEntities];
    public static readonly float[] M14 = new float[Constants.MaxEntities];

    public static readonly float[] M21 = new float[Constants.MaxEntities];
    public static readonly float[] M22 = [.. Enumerable.Repeat(1f, Constants.MaxEntities)];
    public static readonly float[] M23 = new float[Constants.MaxEntities];
    public static readonly float[] M24 = new float[Constants.MaxEntities];

    public static readonly float[] M31 = new float[Constants.MaxEntities];
    public static readonly float[] M32 = new float[Constants.MaxEntities];
    public static readonly float[] M33 = [.. Enumerable.Repeat(1f, Constants.MaxEntities)];
    public static readonly float[] M34 = new float[Constants.MaxEntities];

    public static readonly float[] M41 = new float[Constants.MaxEntities];
    public static readonly float[] M42 = new float[Constants.MaxEntities];
    public static readonly float[] M43 = new float[Constants.MaxEntities];
    public static readonly float[] M44 = [.. Enumerable.Repeat(1f, Constants.MaxEntities)];

    /// <summary>
    /// Creates a WorldTransform for the specified entity.
    /// </summary>
    public static WorldTransformBehavior CreateFor(Entity entity) => new(
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
}
