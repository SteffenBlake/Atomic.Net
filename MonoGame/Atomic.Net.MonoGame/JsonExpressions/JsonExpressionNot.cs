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
[JsonConverter(typeof(JsonExpressionNotConverterFactory))]
public sealed class JsonExpressionNot<TIn, TOut>(JsonExpression<TIn, bool>? operand) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// Expression to negate.
    /// </summary>
    public JsonExpression<TIn, bool>? Operand { get; } = operand;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        if (Operand is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Not: Operand is null"));
            result = null;
            return false;
        }

        if (!Operand.TryCompile(out var operandFunc))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Not: Failed to compile Operand"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var operandExpr = Expression.Invoke(operandFunc, parameter);
        var not = Expression.Not(operandExpr);
        
        result = Expression.Lambda<Func<TIn, TOut>>(not, parameter);
        return true;
    }
}
