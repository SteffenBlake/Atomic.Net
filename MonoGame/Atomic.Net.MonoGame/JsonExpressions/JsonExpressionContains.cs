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
[JsonConverter(typeof(JsonExpressionContainsConverterFactory))]
public sealed class JsonExpressionContains<TIn, TOut>(
    JsonExpression<TIn, TOut>? collection,
    JsonExpression<TIn, TOut>? item
) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// Collection to search in.
    /// </summary>
    public JsonExpression<TIn, TOut>? Collection { get; } = collection;

    /// <summary>
    /// Item to search for.
    /// </summary>
    public JsonExpression<TIn, TOut>? Item { get; } = item;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        if (Collection is null || Item is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Contains: Collection or Item is null"));
            result = null;
            return false;
        }

        if (!Collection.TryCompile(out var collectionFunc) || !Item.TryCompile(out var itemFunc))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Contains: Failed to compile Collection or Item"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var collectionExpr = Expression.Invoke(collectionFunc, parameter);
        var itemExpr = Expression.Invoke(itemFunc, parameter);
        
        var containsMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(itemExpr.Type);
        var contains = Expression.Call(containsMethod, collectionExpr, itemExpr);
        
        result = Expression.Lambda<Func<TIn, TOut>>(contains, parameter);
        return true;
    }
}
