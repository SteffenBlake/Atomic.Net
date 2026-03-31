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
[JsonConverter(typeof(JsonExpressionDivideConverterFactory))]
public sealed class JsonExpressionDivide<TIn, TOut>(
    JsonExpression<TIn, TOut>? dividend,
    JsonExpression<TIn, TOut>? divisor
) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// Dividend (value to be divided).
    /// </summary>
    public JsonExpression<TIn, TOut>? Dividend { get; } = dividend;

    /// <summary>
    /// Divisor (value to divide by).
    /// </summary>
    public JsonExpression<TIn, TOut>? Divisor { get; } = divisor;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        if (Dividend is null || Divisor is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Divide: Dividend or Divisor is null"));
            result = null;
            return false;
        }

        if (!Dividend.TryCompile(out var dividendFunc) || !Divisor.TryCompile(out var divisorFunc))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Divide: Failed to compile Dividend or Divisor"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var dividendExpr = Expression.Invoke(dividendFunc, parameter);
        var divisorExpr = Expression.Invoke(divisorFunc, parameter);
        var divide = Expression.Divide(dividendExpr, divisorExpr);
        
        result = Expression.Lambda<Func<TIn, TOut>>(divide, parameter);
        return true;
    }
}
