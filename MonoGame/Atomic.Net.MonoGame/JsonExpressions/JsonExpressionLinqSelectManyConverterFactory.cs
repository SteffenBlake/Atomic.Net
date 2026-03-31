using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Factory for creating JsonExpressionLinqSelectMany converters.
/// </summary>
public sealed class JsonExpressionLinqSelectManyConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
        {
            return false;
        }

        var genericDefinition = typeToConvert.GetGenericTypeDefinition();
        return genericDefinition == typeof(IJsonExpressionLinqSelectMany<,>);
    }

    public override JsonConverter? CreateConverter(
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var typeArgs = typeToConvert.GetGenericArguments();
        if (typeArgs.Length != 3)
        {
            return null;
        }

        var tIn = typeArgs[0];
        var tSource = typeArgs[1];
        var tResult = typeArgs[2];
        

        var converterType = typeof(JsonExpressionLinqSelectManyConverter<,,>).MakeGenericType(tIn, tSource, tResult);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}
