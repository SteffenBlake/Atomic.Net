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
public sealed class JsonExpressionSymbolMultiply<TIn, TOut>(
    IJsonExpression<TIn, TOut>? left,
    IJsonExpression<TIn, TOut>? right
) : IJsonExpressionSymbolMultiply<TIn, TOut>
{
    /// <summary>
    /// Left multiplicand.
    /// </summary>
    public IJsonExpression<TIn, TOut>? Left { get; } = left;

    /// <summary>
    /// Right multiplicand.
    /// </summary>
    public IJsonExpression<TIn, TOut>? Right { get; } = right;

    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (Left is null || Right is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Multiply: Left or Right operand is null"));
            result = null;
            return false;
        }

        if (!Left.TryCompile(parameter, out var leftExpr) || !Right.TryCompile(parameter, out var rightExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Multiply: Failed to compile Left or Right operand"));
            result = null;
            return false;
        }

        result = Expression.Multiply(leftExpr, rightExpr);
        return true;
    }
}
