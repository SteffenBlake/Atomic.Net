using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores the final world transform of an entity, calculated from inputs and parent hierarchy.
/// </summary>
public struct WorldTransformBehavior
{
    public Matrix Value;

    public WorldTransformBehavior()
    {
        Value = Matrix.Identity;
    }

}

