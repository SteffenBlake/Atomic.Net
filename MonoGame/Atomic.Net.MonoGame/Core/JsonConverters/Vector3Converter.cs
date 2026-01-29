using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Core.JsonConverters;

/// <summary>
/// JSON converter for Vector3 that serializes as object with x, y, z properties.
/// Uses object format {"x": 1, "y": 2, "z": 3} instead of array [1, 2, 3] for
/// better readability, self-documentation, and resilience to property order changes.
/// Zero-allocation implementation using ValueTextEquals for property name comparison.
/// </summary>
public class Vector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            return Vector3.Zero;
        }

        float x = 0, y = 0, z = 0;
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
            else
            {
                reader.Skip();
            }
        }

        return new Vector3(x, y, z);
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteNumber("z", value.Z);
        writer.WriteEndObject();
    }
}
