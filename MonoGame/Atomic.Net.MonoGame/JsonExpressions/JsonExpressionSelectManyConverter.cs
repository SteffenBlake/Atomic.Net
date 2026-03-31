using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionSelectMany.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TSource">Source array element type</typeparam>
/// <typeparam name="TResult">Result element type after flattening</typeparam>
public sealed class JsonExpressionSelectManyConverter<TIn, TSource, TResult> : JsonConverter<JsonExpressionSelectMany<TIn, TSource, TResult>>
{
    public override JsonExpressionSelectMany<TIn, TSource, TResult>? Read(
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
        JsonExpressionSelectMany<TIn, TSource, TResult> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
