using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Core.JsonConverters;

/// <summary>
/// JSON converter for Quaternion that serializes as object with x, y, z, w properties.
/// Uses object format {"x": 0, "y": 0, "z": 0, "w": 1} instead of array [0, 0, 0, 1] for
/// better readability, self-documentation, and resilience to property order changes.
/// Zero-allocation implementation using ValueTextEquals for property name comparison.
/// </summary>
public class QuaternionConverter : JsonConverter<Quaternion>
{
    public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            return Quaternion.Identity;
        }

        float x = 0, y = 0, z = 0, w = 1;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            if (reader.ValueTextEquals("x"u8))
            {
                reader.Read();
                x = reader.GetSingle();
            }
            else if (reader.ValueTextEquals("y"u8))
            {
                reader.Read();
                y = reader.GetSingle();
            }
            else if (reader.ValueTextEquals("z"u8))
            {
                reader.Read();
                z = reader.GetSingle();
            }
            else if (reader.ValueTextEquals("w"u8))
            {
                reader.Read();
                w = reader.GetSingle();
            }
            else
            {
                reader.Skip();
            }
        }

        return new Quaternion(x, y, z, w);
    }

    public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteNumber("z", value.Z);
        writer.WriteNumber("w", value.W);
        writer.WriteEndObject();
    }
}
