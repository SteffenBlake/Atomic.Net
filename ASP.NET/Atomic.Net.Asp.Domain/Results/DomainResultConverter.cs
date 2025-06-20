using System.Text.Json;
using System.Text.Json.Serialization;
using Validly.Details;

namespace Atomic.Net.Asp.Domain.Results;

class DomainResultConverter<T> : JsonConverter<DomainResult<T>>
{
    public override DomainResult<T>? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }

    public override void Write(
        Utf8JsonWriter writer, DomainResult<T> value, JsonSerializerOptions options
    )
    {
        if (value.TryMatch(out T? t))
        {
            JsonSerializer.Serialize(writer, t, options);
            return;
        }
        if (value.TryMatch(out NotFound notFound))
        {
            JsonSerializer.Serialize(writer, notFound, options);
            return;
        }
        if (value.TryMatch(out Conflict conflict))
        {
            JsonSerializer.Serialize(writer, conflict, options);
            return;
        }
        if (value.TryMatch(out Unauthorized unauthorized))
        {
            JsonSerializer.Serialize(writer, unauthorized, options);
            return;
        }
        if (value.TryMatch(out ValidationResultDetails? validationResultDetails))
        {
            JsonSerializer.Serialize(writer, validationResultDetails, options);
            return;
        }

        throw new NotImplementedException();
    }
}
