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
public sealed class JsonExpressionSymbolDivide<TIn, TOut>(
    IJsonExpression<TIn, TOut>? dividend,
    IJsonExpression<TIn, TOut>? divisor
) : IJsonExpressionSymbolDivide<TIn, TOut>
{
    /// <summary>
    /// Dividend (value to be divided).
    /// </summary>
    public IJsonExpression<TIn, TOut>? Dividend { get; } = dividend;

    /// <summary>
    /// Divisor (value to divide by).
    /// </summary>
    public IJsonExpression<TIn, TOut>? Divisor { get; } = divisor;

    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (Dividend is null || Divisor is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Divide: Dividend or Divisor is null"));
            result = null;
            return false;
        }

        if (!Dividend.TryCompile(parameter, out var dividendExpr) || !Divisor.TryCompile(parameter, out var divisorExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Divide: Failed to compile Dividend or Divisor"));
            result = null;
            return false;
        }

        result = Expression.Divide(dividendExpr, divisorExpr);
        return true;
    }
}
