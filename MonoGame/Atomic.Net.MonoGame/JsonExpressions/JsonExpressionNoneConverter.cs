using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionNone.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TSource">Array element type</typeparam>
public sealed class JsonExpressionNoneConverter<TIn, TSource> : JsonConverter<JsonExpressionNone<TIn, TSource>>
{
    public override JsonExpressionNone<TIn, TSource>? Read(
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
        JsonExpressionNone<TIn, TSource> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
