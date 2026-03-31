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
public sealed class JsonExpressionIf<TIn, TOut>(
    IJsonExpression<TIn, bool>? condition,
    IJsonExpression<TIn, TOut>? thenBranch,
    IJsonExpression<TIn, TOut>? elseBranch
) : IJsonExpressionIf<TIn, TOut>
{
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (condition is null || thenBranch is null || elseBranch is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("If: Condition, ThenBranch, or ElseBranch is null"));
            result = null;
            return false;
        }

        if (!condition.TryCompile(parameter, out var conditionExpr) || 
            !thenBranch.TryCompile(parameter, out var thenExpr) || 
            !elseBranch.TryCompile(parameter, out var elseExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("If: Failed to compile Condition, ThenBranch, or ElseBranch"));
            result = null;
            return false;
        }

        result = Expression.Condition(conditionExpr, thenExpr, elseExpr);
        return true;
    }
}
