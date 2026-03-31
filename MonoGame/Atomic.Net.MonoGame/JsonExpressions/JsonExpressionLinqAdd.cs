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
[JsonConverter(typeof(JsonExpressionLinqAddConverterFactory))]
public sealed class JsonExpressionLinqAdd<TIn, TOut>(
    JsonExpression<TIn, TOut>? item,
    JsonExpression<TIn, TOut>? array
) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// Item to add to the array.
    /// </summary>
    public JsonExpression<TIn, TOut>? Item { get; } = item;

    /// <summary>
    /// Array to add the item to.
    /// </summary>
    public JsonExpression<TIn, TOut>? Array { get; } = array;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        if (Item is null || Array is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("LinqAdd: Item or Array is null"));
            result = null;
            return false;
        }

        if (!Item.TryCompile(out var itemFunc) || !Array.TryCompile(out var arrayFunc))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("LinqAdd: Failed to compile Item or Array"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var itemExpr = Expression.Invoke(itemFunc, parameter);
        var arrayExpr = Expression.Invoke(arrayFunc, parameter);
        
        // TOut should be TElement[] where TElement is the array element type
        var elementType = typeof(TOut).IsArray ? typeof(TOut).GetElementType()! : typeof(TOut);
        
        // Call Enumerable.Append(array, item).ToArray()
        var appendMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Append) && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);
        var toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!
            .MakeGenericMethod(elementType);
        
        var appendExpr = Expression.Call(appendMethod, arrayExpr, itemExpr);
        var resultExpr = Expression.Call(toArrayMethod, appendExpr);

        result = Expression.Lambda<Func<TIn, TOut>>(resultExpr, parameter);
        return true;
    }
}
