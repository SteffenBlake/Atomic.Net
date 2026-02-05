using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Core.JsonConverters;

public class InvariantJsonStringEnumConverter<TEnum> : JsonConverter<TEnum?>
    where TEnum : struct, Enum
{

    private static readonly IReadOnlyList<string> KnownValues = [..
        Enum.GetNames<TEnum>().Select(JsonNamingPolicy.CamelCase.ConvertName)
    ];

    public override TEnum? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var value = reader.GetString();

        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var result))
        {
            return result;
        }


        throw new JsonException(
            $"Invalid value '{value}' for enum {typeof(TEnum).Name}, expected one of: ({string.Join(',', KnownValues)})"
        );
    }

    public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var policy = options.PropertyNamingPolicy ?? JsonNamingPolicy.CamelCase;
        var result = policy.ConvertName(value.Value.ToString());
        writer.WriteStringValue(result);
    }
}
