using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores all transform inputs: position, rotation, scale, anchor.
/// </summary>
public struct TransformBehavior : IBehavior<TransformBehavior>
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    public Vector3 Anchor;

    public TransformBehavior()
    {
        Position = Vector3.Zero;
        Rotation = Quaternion.Identity;
        Scale = Vector3.One;
        Anchor = Vector3.Zero;
    }

    public static TransformBehavior CreateFor(Entity entity)
    {
        return new TransformBehavior();
    }
}
