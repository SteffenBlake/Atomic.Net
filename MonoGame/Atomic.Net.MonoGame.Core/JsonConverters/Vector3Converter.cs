using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Core.JsonConverters;

public class Vector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // senior-dev: Use JsonSerializer.Deserialize instead of manual parsing
        var values = JsonSerializer.Deserialize<float[]>(ref reader, options);
        if (values == null || values.Length != 3)
            return Vector3.Zero;
        return new Vector3(values[0], values[1], values[2]);
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Write not needed for M1");
    }
}
