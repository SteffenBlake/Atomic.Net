using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionSelect.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TSource">Source array element type</typeparam>
/// <typeparam name="TResult">Result element type after transformation</typeparam>
public sealed class JsonExpressionSelectConverter<TIn, TSource, TResult> : JsonConverter<JsonExpressionSelect<TIn, TSource, TResult>>
{
    public override JsonExpressionSelect<TIn, TSource, TResult>? Read(
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
        JsonExpressionSelect<TIn, TSource, TResult> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
