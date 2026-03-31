using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Factory for creating JsonExpressionLinqSelect converters.
/// </summary>
public sealed class JsonExpressionLinqSelectConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
        {
            return false;
        }

        var genericDefinition = typeToConvert.GetGenericTypeDefinition();
        return genericDefinition == typeof(IJsonExpressionLinqSelect<,>);
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

        var converterType = typeof(JsonExpressionLinqSelectConverter<,,>).MakeGenericType(tIn, tSource, tResult);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}
