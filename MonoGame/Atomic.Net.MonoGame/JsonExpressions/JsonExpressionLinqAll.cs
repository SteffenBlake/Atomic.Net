using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic all operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by this expression</typeparam>
/// <typeparam name="TElement">Array element type</typeparam>
public sealed class JsonExpressionLinqAll<TIn, TOut, TElement>(
    IJsonExpression<TIn, TElement[]>? source,
    IJsonExpression<TElement, bool>? predicate
) : IJsonExpressionLinqAll<TIn, TOut>
{
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (source is null || predicate is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("All: Source or Predicate is null"));
            result = null;
            return false;
        }

        if (!source.TryCompile(parameter, out var sourceExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("All: Failed to compile Source or Predicate"));
            result = null;
            return false;
        }

        var itemParam = Expression.Parameter(typeof(TElement), "item");
        if (!predicate.TryCompile(itemParam, out var predicateBody))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("All: Failed to compile Source or Predicate"));
            result = null;
            return false;
        }

        var predicateFunc = Expression.Lambda<Func<TElement, bool>>(predicateBody, itemParam);
        
        // Call Enumerable.All(source, predicate)
        // JSONLogic semantics: empty array returns false (not vacuously true)
        var allMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.All) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TElement));
        
        var anyMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 1)
            .MakeGenericMethod(typeof(TElement));

        // source.Any() && source.All(predicate)
        var hasElements = Expression.Call(anyMethod, sourceExpr);
        var allMatches = Expression.Call(allMethod, sourceExpr, predicateFunc);
        result = Expression.AndAlso(hasElements, allMatches);
        return true;
    }
}
