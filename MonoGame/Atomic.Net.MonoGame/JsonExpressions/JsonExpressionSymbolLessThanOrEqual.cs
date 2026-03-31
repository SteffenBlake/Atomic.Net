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
/// <typeparam name="TCompare">Type of values being compared</typeparam>
/// <typeparam name="TOut">Output type produced by this expression</typeparam>
public sealed class JsonExpressionSymbolLessThanOrEqual<TIn, TCompare, TOut>(
    IJsonExpression<TIn, TCompare>? left,
    IJsonExpression<TIn, TCompare>? right
) : IJsonExpressionSymbolLessThanOrEqual<TIn, TOut>
{
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (left is null || right is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("LessThanOrEqual: Left or Right operand is null"));
            result = null;
            return false;
        }

        if (!left.TryCompile(parameter, out var leftExpr) || !right.TryCompile(parameter, out var rightExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("LessThanOrEqual: Failed to compile Left or Right operand"));
            result = null;
            return false;
        }

        result = Expression.LessThanOrEqual(leftExpr, rightExpr);
        return true;
    }
}
