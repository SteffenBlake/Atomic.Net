using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Core.JsonConverters;

public class QuaternionConverter : JsonConverter<Quaternion>
{
    public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var values = JsonSerializer.Deserialize<float[]>(ref reader, options);
        if (values == null || values.Length != 4)
        {
            return Quaternion.Identity;
        }

        return new Quaternion(values[0], values[1], values[2], values[3]);
    }

    public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
    {
        // senior-dev: Serialize as [x, y, z, w] array
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteNumberValue(value.W);
        writer.WriteEndArray();
    }
}
