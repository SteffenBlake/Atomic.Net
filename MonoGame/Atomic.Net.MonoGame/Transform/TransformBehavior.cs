using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Core.JsonConverters;
using Microsoft.Xna.Framework;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// Stores all transform inputs: position, rotation, scale, anchor.
/// </summary>
public readonly struct TransformBehavior : IBehavior<TransformBehavior>
{
    private readonly Vector3? _position;

    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Position
    {
        init => _position = value;
        get => _position ?? Vector3.Zero;
    }

    private readonly Quaternion? _rotation;
    [JsonConverter(typeof(QuaternionConverter))]
    public Quaternion Rotation
    {
        init => _rotation = value;
        get => _rotation ?? Quaternion.Identity;
    }
   

    private readonly Vector3? _scale;

    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Scale
    {
        init => _scale = value;
        get => _scale ?? Vector3.One;
    }
    
   
    private readonly Vector3? _anchor;

    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Anchor
    {
        init => _anchor = value;
        get => _anchor ?? Vector3.Zero;
    }

    public static TransformBehavior CreateFor(Entity entity)
    {
        return new TransformBehavior();
    }
}
