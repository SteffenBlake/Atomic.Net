using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Core.JsonConverters;

public class Vector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Write not needed for M1");
    }
}
