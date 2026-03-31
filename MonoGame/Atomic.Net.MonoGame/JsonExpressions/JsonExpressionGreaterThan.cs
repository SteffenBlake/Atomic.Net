using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic '>' operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by this expression</typeparam>
[JsonConverter(typeof(JsonExpressionGreaterThanConverterFactory))]
public sealed class JsonExpressionGreaterThan<TIn, TOut> : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// Left side of comparison.
    /// </summary>
    public JsonExpression<TIn, TOut>? Left { get; set; }

    /// <summary>
    /// Right side of comparison.
    /// </summary>
    public JsonExpression<TIn, TOut>? Right { get; set; }

    public override bool TryCompile(
        JsonDocument rule,
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        throw new NotImplementedException();
    }
}
