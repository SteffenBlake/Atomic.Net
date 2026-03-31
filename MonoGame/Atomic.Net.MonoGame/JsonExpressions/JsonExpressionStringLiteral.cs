using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic string literal expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Requested output type</typeparam>
[JsonConverter(typeof(JsonExpressionStringLiteralConverterFactory))]
public sealed class JsonExpressionStringLiteral<TIn, TOut> : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// The literal string value.
    /// </summary>
    public string? Value { get; set; }

    public override bool TryCompile(
        JsonDocument rule,
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        throw new NotImplementedException();
    }
}
