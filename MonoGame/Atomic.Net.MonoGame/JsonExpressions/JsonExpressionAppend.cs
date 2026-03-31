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
[JsonConverter(typeof(JsonExpressionAppendConverterFactory))]
public sealed class JsonExpressionAppend<TIn, TOut>(
    JsonExpression<TIn, TOut>? first,
    JsonExpression<TIn, TOut>? second
) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// First array expression.
    /// </summary>
    public JsonExpression<TIn, TOut>? First { get; } = first;

    /// <summary>
    /// Second array expression to concatenate.
    /// </summary>
    public JsonExpression<TIn, TOut>? Second { get; } = second;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        if (First is null || Second is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Append: First or Second is null"));
            result = null;
            return false;
        }

        if (!First.TryCompile(out var firstFunc) || !Second.TryCompile(out var secondFunc))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Append: Failed to compile First or Second"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var firstExpr = Expression.Invoke(firstFunc, parameter);
        var secondExpr = Expression.Invoke(secondFunc, parameter);
        
        // TOut should be TElement[] where TElement is the array element type
        var elementType = typeof(TOut).IsArray ? typeof(TOut).GetElementType()! : typeof(TOut);
        
        // Call Enumerable.Concat(first, second).ToArray()
        var concatMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Concat) && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);
        var toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!
            .MakeGenericMethod(elementType);
        
        var concatExpr = Expression.Call(concatMethod, firstExpr, secondExpr);
        var resultExpr = Expression.Call(toArrayMethod, concatExpr);

        result = Expression.Lambda<Func<TIn, TOut>>(resultExpr, parameter);
        return true;
    }
}
