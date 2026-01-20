using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace Atomic.Net.MonoGame.Core.JsonConverters;

public class QuaternionConverter : JsonConverter<Quaternion>
{
    public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

    public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Write not needed for M1");
    }
}
