using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Atomic.Net.MonoGame.Core.JsonConverters;

namespace Atomic.Net.MonoGame.Transform.JsonConverters;

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
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            return new TransformBehavior();
        }

        var transform = new TransformBehavior();
        
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return transform;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName?.ToLowerInvariant())
            {
                case "position":
                    // senior-dev: Use JsonSerializer.Deserialize with Vector3Converter
                    transform.Position = JsonSerializer.Deserialize<Vector3>(ref reader, options);
                    break;
                case "rotation":
                    // senior-dev: Use JsonSerializer.Deserialize with QuaternionConverter
                    transform.Rotation = JsonSerializer.Deserialize<Quaternion>(ref reader, options);
                    break;
                case "scale":
                    // senior-dev: Use JsonSerializer.Deserialize with Vector3Converter
                    transform.Scale = JsonSerializer.Deserialize<Vector3>(ref reader, options);
                    break;
                case "anchor":
                    // senior-dev: Use JsonSerializer.Deserialize with Vector3Converter
                    transform.Anchor = JsonSerializer.Deserialize<Vector3>(ref reader, options);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return transform;
    }

    public override void Write(Utf8JsonWriter writer, TransformBehavior value, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Write not needed for M1");
    }
}
