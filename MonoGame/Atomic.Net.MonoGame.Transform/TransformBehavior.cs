using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Core.JsonConverters;
using Microsoft.Xna.Framework;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores all transform inputs: position, rotation, scale, anchor.
/// </summary>
public record struct TransformBehavior : IBehavior<TransformBehavior>
{
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Position { get; set; }
    
    [JsonConverter(typeof(QuaternionConverter))]
    public Quaternion Rotation { get; set; }
    
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Scale { get; set; }
    
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Anchor { get; set; }

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
