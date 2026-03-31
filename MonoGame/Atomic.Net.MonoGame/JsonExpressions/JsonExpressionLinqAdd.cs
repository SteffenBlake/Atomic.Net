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
/// <typeparam name="TOut">Output type produced by this expression (the array type)</typeparam>
/// <typeparam name="TElement">Element type of the array</typeparam>
public sealed class JsonExpressionLinqAdd<TIn, TOut, TElement>(
    IJsonExpression<TIn, TElement>? item,
    IJsonExpression<TIn, TOut>? array
) : IJsonExpressionLinqAdd<TIn, TOut>
{
    /// <summary>
    /// Item to add to the array.
    /// </summary>
    public IJsonExpression<TIn, TElement>? Item { get; } = item;

    /// <summary>
    /// Array to add the item to.
    /// </summary>
    public IJsonExpression<TIn, TOut>? Array { get; } = array;

    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (Item is null || Array is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("LinqAdd: Item or Array is null"));
            result = null;
            return false;
        }

        if (!Item.TryCompile(parameter, out var itemExpr) || !Array.TryCompile(parameter, out var arrayExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("LinqAdd: Failed to compile Item or Array"));
            result = null;
            return false;
        }
        
        // TOut should be TElement[] where TElement is the array element type
        var elementType = typeof(TElement);
        
        // Call Enumerable.Append(array, item).ToArray()
        var appendMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Append) && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);
        var toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!
            .MakeGenericMethod(elementType);
        
        var appendExpr = Expression.Call(appendMethod, arrayExpr, itemExpr);
        result = Expression.Call(toArrayMethod, appendExpr);
        return true;
    }
}
