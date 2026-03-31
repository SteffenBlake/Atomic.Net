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
public sealed class JsonExpressionIn<TIn, TOut, TElement>(
    IJsonExpression<TIn, TElement[]>? collection,
    IJsonExpression<TIn, TElement>? item
) : IJsonExpressionIn<TIn, TOut>
{
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (collection is null || item is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("In: Collection or Item is null"));
            result = null;
            return false;
        }

        if (!collection.TryCompile(parameter, out var collectionExpr) || !item.TryCompile(parameter, out var itemExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("In: Failed to compile Collection or Item"));
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
