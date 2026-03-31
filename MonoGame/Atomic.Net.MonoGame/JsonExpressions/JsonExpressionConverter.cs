using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Base JSON converter for JsonExpression types.
/// Handles deserialization of JSONLogic rules into typed expression instances.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public abstract class JsonExpressionConverter<TIn, TOut> : JsonConverter<JsonExpression<TIn, TOut>>
{
    /// <summary>
    /// Reads and converts JSON to a JsonExpression instance.
    /// </summary>
    public override JsonExpression<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // TODO: Implement JSON parsing logic
        // - Parse JSONLogic operator structure
        // - Create appropriate expression instance
        throw new NotImplementedException();
    }

    /// <summary>
    /// Writes a JsonExpression instance to JSON.
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        JsonExpression<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
