using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic array literal expression.
/// </summary>
/// <typeparam name="TIn">Input datatype</typeparam>
/// <typeparam name="TOut">Output array type (e.g. float[])</typeparam>
/// <typeparam name="TElement">Element type of the array (e.g. float)</typeparam>
public sealed class JsonExpressionArrayLiteral<TIn, TOut, TElement>(
    IJsonExpression<TIn, TElement>[]? elements
) : IJsonExpressionArrayLiteral<TIn, TOut>
{
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (elements is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("ArrayLiteral: Value is null"));
            result = null;
            return false;
        }

        var elementExpressions = new List<Expression>();

        // Compile each element expression
        foreach (var element in elements)
        {
            if (element is null || !element.TryCompile(parameter, out var elementExpr))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("ArrayLiteral: Element is null or failed to compile"));
                result = null;
                return false;
            }

            elementExpressions.Add(elementExpr);
        }

        result = Expression.NewArrayInit(typeof(TElement), elementExpressions);
        return true;
    }
}
