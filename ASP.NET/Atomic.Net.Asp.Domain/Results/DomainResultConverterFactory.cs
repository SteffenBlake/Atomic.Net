using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.Asp.Domain.Results;

public class DomainResultConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return 
            typeToConvert.IsGenericType && 
            typeToConvert.GetGenericTypeDefinition() == typeof(DomainResult<>);
    }

    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
    {
        var elementType = type.GetGenericArguments()[0];
        var converterType = typeof(DomainResultConverter<>).MakeGenericType(elementType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
