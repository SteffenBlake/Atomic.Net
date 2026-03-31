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
[JsonConverter(typeof(JsonExpressionMinConverterFactory))]
public sealed class JsonExpressionMin<TIn, TOut>(JsonExpression<TIn, TOut>[]? operands) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// Array of values to compare.
    /// </summary>
    public JsonExpression<TIn, TOut>[]? Operands { get; } = operands;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        if (Operands is null || Operands.Length == 0)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Min: Operands array is null or empty"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var operandExprs = new List<Expression>();

        foreach (var operand in Operands)
        {
            if (operand is null || !operand.TryCompile(out var operandFunc))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("Min: Operand is null or failed to compile"));
                result = null;
                return false;
            }

            operandExprs.Add(Expression.Invoke(operandFunc, parameter));
        }

        // Build nested Math.Min calls
        Expression minExpr = operandExprs[0];
        var minMethod = typeof(Math).GetMethod(nameof(Math.Min), [minExpr.Type, minExpr.Type])!;
        
        for (int i = 1; i < operandExprs.Count; i++)
        {
            minExpr = Expression.Call(minMethod, minExpr, operandExprs[i]);
        }

        result = Expression.Lambda<Func<TIn, TOut>>(minExpr, parameter);
        return true;
    }
}
