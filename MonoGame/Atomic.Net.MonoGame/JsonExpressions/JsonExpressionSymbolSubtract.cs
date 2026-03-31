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
public sealed class JsonExpressionSymbolSubtract<TIn, TOut>(
    IJsonExpression<TIn, TOut>? minuend,
    IJsonExpression<TIn, TOut>? subtrahend
) : IJsonExpressionSymbolSubtract<TIn, TOut>
{
    /// <summary>
    /// Minuend (value to subtract from).
    /// </summary>
    public IJsonExpression<TIn, TOut>? Minuend { get; } = minuend;

    /// <summary>
    /// Subtrahend (value to subtract).
    /// </summary>
    public IJsonExpression<TIn, TOut>? Subtrahend { get; } = subtrahend;

    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (Minuend is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Subtract: Minuend is null"));
            result = null;
            return false;
        }

        if (!Minuend.TryCompile(parameter, out var minuendExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Subtract: Failed to compile Minuend"));
            result = null;
            return false;
        }

        // Unary negation: {"-": value}
        if (Subtrahend is null)
        {
            result = Expression.Negate(minuendExpr);
            return true;
        }

        if (!Subtrahend.TryCompile(parameter, out var subtrahendExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Subtract: Failed to compile Subtrahend"));
            result = null;
            return false;
        }

        result = Expression.Subtract(minuendExpr, subtrahendExpr);
        return true;
    }
}
