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
[JsonConverter(typeof(JsonExpressionIfConverterFactory))]
public sealed class JsonExpressionIf<TIn, TOut>(
    JsonExpression<TIn, bool>? condition,
    JsonExpression<TIn, TOut>? thenBranch,
    JsonExpression<TIn, TOut>? elseBranch
) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// Condition expression.
    /// </summary>
    public JsonExpression<TIn, bool>? Condition { get; } = condition;

    /// <summary>
    /// Expression to evaluate if condition is true.
    /// </summary>
    public JsonExpression<TIn, TOut>? ThenBranch { get; } = thenBranch;

    /// <summary>
    /// Expression to evaluate if condition is false.
    /// </summary>
    public JsonExpression<TIn, TOut>? ElseBranch { get; } = elseBranch;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        if (Condition is null || ThenBranch is null || ElseBranch is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("If: Condition, ThenBranch, or ElseBranch is null"));
            result = null;
            return false;
        }

        if (!Condition.TryCompile(out var conditionFunc) || 
            !ThenBranch.TryCompile(out var thenFunc) || 
            !ElseBranch.TryCompile(out var elseFunc))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("If: Failed to compile Condition, ThenBranch, or ElseBranch"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var conditionExpr = Expression.Invoke(conditionFunc, parameter);
        var thenExpr = Expression.Invoke(thenFunc, parameter);
        var elseExpr = Expression.Invoke(elseFunc, parameter);
        var conditional = Expression.Condition(conditionExpr, thenExpr, elseExpr);
        
        result = Expression.Lambda<Func<TIn, TOut>>(conditional, parameter);
        return true;
    }
}
