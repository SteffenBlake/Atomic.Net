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
public sealed class JsonExpressionMax<TIn, TOut>(IJsonExpression<TIn, TOut>[]? operands) : IJsonExpressionMax<TIn, TOut>
{
    /// <summary>
    /// Array of values to compare.
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
            EventBus<ErrorEvent>.Push(new ErrorEvent("Max: Operands array is null or empty"));
            result = null;
            return false;
        }

        var operandExprs = new List<Expression>();

        foreach (var operand in Operands)
        {
            if (operand is null || !operand.TryCompile(parameter, out var operandExpr))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("Max: Operand is null or failed to compile"));
                result = null;
                return false;
            }

            operandExprs.Add(operandExpr);
        }

        // Build nested Math.Max calls
        Expression maxExpr = operandExprs[0];
        var maxMethod = typeof(Math).GetMethod(nameof(Math.Max), [maxExpr.Type, maxExpr.Type])!;
        
        for (int i = 1; i < operandExprs.Count; i++)
        {
            maxExpr = Expression.Call(maxMethod, maxExpr, operandExprs[i]);
        }

        result = maxExpr;
        return true;
    }
}
