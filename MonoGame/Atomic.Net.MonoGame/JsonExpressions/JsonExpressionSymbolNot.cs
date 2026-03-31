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
public sealed class JsonExpressionSymbolNot<TIn, TOut>(
    IJsonExpression<TIn, bool>? operand
) : IJsonExpressionSymbolNot<TIn, TOut>
{
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (operand is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Not: Operand is null"));
            result = null;
            return false;
        }

        if (!operand.TryCompile(parameter, out var operandExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Not: Failed to compile Operand"));
            result = null;
            return false;
        }

        result = Expression.Not(operandExpr);
        return true;
    }
}
