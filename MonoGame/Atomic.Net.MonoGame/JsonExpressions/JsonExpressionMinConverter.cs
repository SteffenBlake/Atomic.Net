using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionMin.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionMinConverter<TIn, TOut> : JsonConverter<JsonExpressionMin<TIn, TOut>>
{
    public override JsonExpressionMin<TIn, TOut>? Read(
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
        JsonExpressionMin<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
