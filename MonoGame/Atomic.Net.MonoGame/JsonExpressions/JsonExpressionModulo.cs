using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by this expression</typeparam>
[JsonConverter(typeof(JsonExpressionModuloConverterFactory))]
public sealed class JsonExpressionModulo<TIn, TOut>(
    JsonExpression<TIn, TOut>? dividend,
    JsonExpression<TIn, TOut>? divisor
) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// Dividend (value to be divided).
    /// </summary>
    public JsonExpression<TIn, TOut>? Dividend { get; init; } = dividend;

    /// <summary>
    /// Divisor (modulo value).
    /// </summary>
    public JsonExpression<TIn, TOut>? Divisor { get; init; } = divisor;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        if (Dividend is null || Divisor is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Modulo: Dividend or Divisor is null"));
            result = null;
            return false;
        }

        if (!Dividend.TryCompile(out var dividendFunc) || !Divisor.TryCompile(out var divisorFunc))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Modulo: Failed to compile Dividend or Divisor"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var dividendExpr = Expression.Invoke(dividendFunc, parameter);
        var divisorExpr = Expression.Invoke(divisorFunc, parameter);
        var modulo = Expression.Modulo(dividendExpr, divisorExpr);
        
        result = Expression.Lambda<Func<TIn, TOut>>(modulo, parameter);
        return true;
    }
}
