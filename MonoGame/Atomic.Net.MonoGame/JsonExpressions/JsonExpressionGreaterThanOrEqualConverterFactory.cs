using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Factory for creating JsonExpressionGreaterThanOrEqual converters.
/// </summary>
public sealed class JsonExpressionGreaterThanOrEqualConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
        {
            return false;
        }

        var genericDefinition = typeToConvert.GetGenericTypeDefinition();
        return genericDefinition == typeof(JsonExpressionGreaterThanOrEqual<,>);
    }

    public override JsonConverter? CreateConverter(
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var typeArgs = typeToConvert.GetGenericArguments();
        if (typeArgs.Length != 2)
        {
            return null;
        }

        var tIn = typeArgs[0];
        var tOut = typeArgs[1];

        var converterType = typeof(JsonExpressionGreaterThanOrEqualConverter<,>).MakeGenericType(tIn, tOut);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}
