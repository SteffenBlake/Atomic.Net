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
public sealed class JsonExpressionAnd<TIn, TOut>(IJsonExpression<TIn, TOut>[]? operands) : IJsonExpressionAnd<TIn, TOut>
{
    /// <summary>
    /// Array of conditions to evaluate (returns last truthy value or first falsy).
    /// </summary>
    public IJsonExpression<TIn, TOut>[]? Operands { get; } = operands;

    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (Operands is null || Operands.Length == 0)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("And: Operands array is null or empty"));
            result = null;
            return false;
        }

        Expression? andExpr = null;

        foreach (var operand in Operands)
        {
            if (operand is null || !operand.TryCompile(parameter, out var operandExpr))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("And: Operand is null or failed to compile"));
                result = null;
                return false;
            }

            andExpr = andExpr is null ? operandExpr : Expression.AndAlso(andExpr, operandExpr);
        }

        result = andExpr!;
        return true;
    }
}
