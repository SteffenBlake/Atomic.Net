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
        // test-architect: Stub - To be implemented by #senior-dev
        // senior-dev: Implementing JSON deserialization for TransformBehavior
        // senior-dev: Missing fields use C# defaults (Position/Anchor: Zero, Rotation: Identity, Scale: One)
        // senior-dev: Returns default behavior on invalid data (graceful degradation)
        
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            // senior-dev: Invalid data - return default behavior
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

            try
            {
                switch (propertyName?.ToLowerInvariant())
                {
                    case "position":
                        transform.Position = ReadVector3(ref reader);
                        break;
                    case "rotation":
                        transform.Rotation = ReadQuaternion(ref reader);
                        break;
                    case "scale":
                        transform.Scale = ReadVector3(ref reader);
                        break;
                    case "anchor":
                        transform.Anchor = ReadVector3(ref reader);
                        break;
                    default:
                        // senior-dev: Skip unknown properties
                        reader.Skip();
                        break;
                }
            }
            catch
            {
                // senior-dev: On invalid data, skip property and continue (graceful degradation)
                reader.Skip();
            }
        }

        return transform;
    }

    private static Vector3 ReadVector3(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            return Vector3.Zero;
        }

        var values = new float[3];
        var index = 0;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (index < 3 && reader.TokenType == JsonTokenType.Number)
            {
                values[index++] = reader.GetSingle();
            }
        }

        return new Vector3(values[0], values[1], values[2]);
    }

    private static Quaternion ReadQuaternion(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            return Quaternion.Identity;
        }

        var values = new float[4];
        var index = 0;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (index < 4 && reader.TokenType == JsonTokenType.Number)
            {
                values[index++] = reader.GetSingle();
            }
        }

        return new Quaternion(values[0], values[1], values[2], values[3]);
    }

    public override void Write(Utf8JsonWriter writer, TransformBehavior value, JsonSerializerOptions options)
    {
        // test-architect: Not needed for M1 (only loading, not saving)
        throw new NotImplementedException("Write not needed for M1");
    }
}
