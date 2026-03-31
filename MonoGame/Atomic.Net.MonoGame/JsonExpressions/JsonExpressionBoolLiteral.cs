using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic boolean literal expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Requested output type</typeparam>
[JsonConverter(typeof(JsonExpressionBoolLiteralConverterFactory))]
public sealed class JsonExpressionBoolLiteral<TIn, TOut> : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// The literal boolean value.
    /// </summary>
    public bool Value { get; set; }

    public override bool TryCompile(
        JsonDocument rule,
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        throw new NotImplementedException();
    }
}
