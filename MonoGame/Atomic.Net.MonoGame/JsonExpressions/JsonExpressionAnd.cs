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
public sealed class JsonExpressionAnd<TIn, TOut>(
    IJsonExpression<TIn, TOut>[]? operands
) : IJsonExpressionAnd<TIn, TOut>
{
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (operands is null || operands.Length != 2)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("And: Requires exactly 2 operands"));
            result = null;
            return false;
        }

        if (!operands[0].TryCompile(parameter, out var leftExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("And: Left operand failed to compile"));
            result = null;
            return false;
        }

        if (!operands[1].TryCompile(parameter, out var rightExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("And: Right operand failed to compile"));
            result = null;
            return false;
        }

        result = Expression.AndAlso(leftExpr, rightExpr);
        return true;
    }
}