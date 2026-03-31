using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic '>' operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by this expression</typeparam>
[JsonConverter(typeof(JsonExpressionGreaterThanConverterFactory))]
public sealed class JsonExpressionGreaterThan<TIn, TOut>(
    JsonExpression<TIn, float>? left,
    JsonExpression<TIn, float>? right
) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// Left side of comparison.
    /// </summary>
    public JsonExpression<TIn, float>? Left { get; } = left;

    /// <summary>
    /// Right side of comparison.
    /// </summary>
    public JsonExpression<TIn, float>? Right { get; } = right;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        if (Left is null || Right is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("GreaterThan: Left or Right operand is null"));
            result = null;
            return false;
        }

        if (!Left.TryCompile(out var leftFunc) || !Right.TryCompile(out var rightFunc))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("GreaterThan: Failed to compile Left or Right operand"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var leftExpr = Expression.Invoke(leftFunc, parameter);
        var rightExpr = Expression.Invoke(rightFunc, parameter);
        var comparison = Expression.GreaterThan(leftExpr, rightExpr);
        
        result = Expression.Lambda<Func<TIn, TOut>>(comparison, parameter);
        return true;
    }
}
