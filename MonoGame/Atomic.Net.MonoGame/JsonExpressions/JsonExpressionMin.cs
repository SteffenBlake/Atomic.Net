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
public sealed class JsonExpressionMin<TIn, TOut>(
    IJsonExpression<TIn, TOut>[]? operands
) : IJsonExpressionMin<TIn, TOut>
{
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (operands is null || operands.Length == 0)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Min: Operands array is null or empty"));
            result = null;
            return false;
        }

        var operandExprs = new List<Expression>();

        foreach (var operand in operands)
        {
            if (operand is null || !operand.TryCompile(parameter, out var operandExpr))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("Min: Operand is null or failed to compile"));
                result = null;
                return false;
            }

            operandExprs.Add(operandExpr);
        }

        // Build nested Math.Min calls
        Expression minExpr = operandExprs[0];
        var minMethod = typeof(Math).GetMethod(nameof(Math.Min), [minExpr.Type, minExpr.Type])!;
        
        for (int i = 1; i < operandExprs.Count; i++)
        {
            minExpr = Expression.Call(minMethod, minExpr, operandExprs[i]);
        }

        result = minExpr;
        return true;
    }
}
