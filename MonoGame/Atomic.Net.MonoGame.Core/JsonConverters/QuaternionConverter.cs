using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Core.JsonConverters;

public class QuaternionConverter : JsonConverter<Quaternion>
{
    public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // senior-dev: Use JsonSerializer.Deserialize instead of manual parsing
        var values = JsonSerializer.Deserialize<float[]>(ref reader, options);
        if (values == null || values.Length != 4)
            return Quaternion.Identity;
        return new Quaternion(values[0], values[1], values[2], values[3]);
    }

    public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Write not needed for M1");
    }
}
