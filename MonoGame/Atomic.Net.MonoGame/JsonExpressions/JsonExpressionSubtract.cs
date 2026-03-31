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
[JsonConverter(typeof(JsonExpressionSubtractConverterFactory))]
public sealed class JsonExpressionSubtract<TIn, TOut>(
    JsonExpression<TIn, TOut>? minuend,
    JsonExpression<TIn, TOut>? subtrahend
) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// Minuend (value to subtract from).
    /// </summary>
    public JsonExpression<TIn, TOut>? Minuend { get; } = minuend;

    /// <summary>
    /// Subtrahend (value to subtract).
    /// </summary>
    public JsonExpression<TIn, TOut>? Subtrahend { get; } = subtrahend;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        if (Minuend is null || Subtrahend is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Subtract: Minuend or Subtrahend is null"));
            result = null;
            return false;
        }

        if (!Minuend.TryCompile(out var minuendFunc) || !Subtrahend.TryCompile(out var subtrahendFunc))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Subtract: Failed to compile Minuend or Subtrahend"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var minuendExpr = Expression.Invoke(minuendFunc, parameter);
        var subtrahendExpr = Expression.Invoke(subtrahendFunc, parameter);
        var subtract = Expression.Subtract(minuendExpr, subtrahendExpr);
        
        result = Expression.Lambda<Func<TIn, TOut>>(subtract, parameter);
        return true;
    }
}
