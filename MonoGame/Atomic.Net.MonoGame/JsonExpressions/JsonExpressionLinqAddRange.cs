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
public sealed class JsonExpressionLinqAddRange<TIn, TOut>(
    IJsonExpression<TIn, TOut>? first,
    IJsonExpression<TIn, TOut>? second
) : IJsonExpressionLinqAddRange<TIn, TOut>
{
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (first is null || second is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Append: First or Second is null"));
            result = null;
            return false;
        }

        if (!first.TryCompile(parameter, out var firstExpr) || !second.TryCompile(parameter, out var secondExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Append: Failed to compile First or Second"));
            result = null;
            return false;
        }
        
        // TOut should be TElement[] where TElement is the array element type
        var elementType = typeof(TOut).IsArray ? typeof(TOut).GetElementType()! : typeof(TOut);
        
        // Call Enumerable.Concat(first, second).ToArray()
        var concatMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Concat) && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);
        var toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!
            .MakeGenericMethod(elementType);
        
        var concatExpr = Expression.Call(concatMethod, firstExpr, secondExpr);
        result = Expression.Call(toArrayMethod, concatExpr);
        return true;
    }
}
