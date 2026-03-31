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
public sealed class JsonExpressionSymbolModulo<TIn, TOut>(
    IJsonExpression<TIn, TOut>? dividend,
    IJsonExpression<TIn, TOut>? divisor
) : IJsonExpressionSymbolModulo<TIn, TOut>
{
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (dividend is null || divisor is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Modulo: Dividend or Divisor is null"));
            result = null;
            return false;
        }

        if (!dividend.TryCompile(parameter, out var dividendExpr) || !divisor.TryCompile(parameter, out var divisorExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Modulo: Failed to compile Dividend or Divisor"));
            result = null;
            return false;
        }

        result = Expression.Modulo(dividendExpr, divisorExpr);
        return true;
    }
}
