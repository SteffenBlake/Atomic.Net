using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionArrayLiteral.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Requested output type</typeparam>
public sealed class JsonExpressionArrayLiteralConverter<TIn, TOut> : JsonConverter<JsonExpressionArrayLiteral<TIn, TOut>>
{
    public override JsonExpressionArrayLiteral<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // TODO: Implement JSON parsing logic
        throw new NotImplementedException();
    }

    public override void Write(
        Utf8JsonWriter writer,
        JsonExpressionArrayLiteral<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
