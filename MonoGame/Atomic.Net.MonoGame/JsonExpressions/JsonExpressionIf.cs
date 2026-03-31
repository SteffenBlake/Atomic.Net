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
    /// <summary>
    /// Condition expression.
    /// </summary>
    public IJsonExpression<TIn, bool>? Condition { get; } = condition;

    /// <summary>
    /// Expression to evaluate if condition is true.
    /// </summary>
    public IJsonExpression<TIn, TOut>? ThenBranch { get; } = thenBranch;

    /// <summary>
    /// Expression to evaluate if condition is false.
    /// </summary>
    public IJsonExpression<TIn, TOut>? ElseBranch { get; } = elseBranch;

    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (Condition is null || ThenBranch is null || ElseBranch is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("If: Condition, ThenBranch, or ElseBranch is null"));
            result = null;
            return false;
        }

        if (!Condition.TryCompile(parameter, out var conditionExpr) || 
            !ThenBranch.TryCompile(parameter, out var thenExpr) || 
            !ElseBranch.TryCompile(parameter, out var elseExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("If: Failed to compile Condition, ThenBranch, or ElseBranch"));
            result = null;
            return false;
        }

        result = Expression.Condition(conditionExpr, thenExpr, elseExpr);
        return true;
    }
}
