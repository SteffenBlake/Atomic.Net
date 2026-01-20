using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Transform;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Scenes.JsonConverters;

/// <summary>
/// Custom JSON converter for TransformBehavior struct.
/// Deserializes transform data directly into TransformBehavior.
/// Handles Position (Vector3), Rotation (Quaternion), Scale (Vector3), Anchor (Vector3).
/// Missing fields use C# defaults (Position/Anchor: Zero, Rotation: Identity, Scale: One).
/// Returns default behavior on invalid data (graceful degradation).
/// </summary>
public class TransformBehaviorConverter : JsonConverter<TransformBehavior>
{
    public override TransformBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // test-architect: Stub - To be implemented by @senior-dev
        throw new NotImplementedException("To be implemented by @senior-dev");
    }

    public override void Write(Utf8JsonWriter writer, TransformBehavior value, JsonSerializerOptions options)
    {
        // test-architect: Not needed for M1 (only loading, not saving)
        throw new NotImplementedException("Write not needed for M1");
    }
}
