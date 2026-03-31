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
[JsonConverter(typeof(JsonExpressionSymbolAddConverterFactory))]
public sealed class JsonExpressionSymbolAdd<TIn, TOut>(JsonExpression<TIn, TOut>[]? operands) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// Array of values to add/concatenate.
    /// </summary>
    public JsonExpression<TIn, TOut>[]? Operands { get; } = operands;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        if (Operands is null || Operands.Length == 0)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("SymbolAdd: Operands array is null or empty"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        Expression? addExpr = null;

        foreach (var operand in Operands)
        {
            if (operand is null || !operand.TryCompile(out var operandFunc))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("SymbolAdd: Operand is null or failed to compile"));
                result = null;
                return false;
            }

            var operandExpr = Expression.Invoke(operandFunc, parameter);
            addExpr = addExpr is null ? operandExpr : Expression.Add(addExpr, operandExpr);
        }

        result = Expression.Lambda<Func<TIn, TOut>>(addExpr!, parameter);
        return true;
    }
}
