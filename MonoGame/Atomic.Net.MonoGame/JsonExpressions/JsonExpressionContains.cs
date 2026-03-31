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
/// <typeparam name="TElement">Element type of the collection</typeparam>
public sealed class JsonExpressionContains<TIn, TOut, TElement>(
    IJsonExpression<TIn, TElement[]>? collection,
    IJsonExpression<TIn, TElement>? item
) : IJsonExpressionContains<TIn, TOut>
{
    /// <summary>
    /// Collection to search in.
    /// </summary>
    public IJsonExpression<TIn, TElement[]>? Collection { get; } = collection;

    /// <summary>
    /// Item to search for.
    /// </summary>
    public IJsonExpression<TIn, TElement>? Item { get; } = item;

    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (Collection is null || Item is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Contains: Collection or Item is null"));
            result = null;
            return false;
        }

        if (!Collection.TryCompile(parameter, out var collectionExpr) || !Item.TryCompile(parameter, out var itemExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Contains: Failed to compile Collection or Item"));
            result = null;
            return false;
        }

        var containsMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TElement));
        
        result = Expression.Call(containsMethod, collectionExpr, itemExpr);
        return true;
    }
}
