using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic where (filter) operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by this expression (the filtered array type)</typeparam>
/// <typeparam name="TElement">Array element type</typeparam>
public sealed class JsonExpressionLinqWhere<TIn, TOut, TElement>(
    IJsonExpression<TIn, TElement[]>? source,
    IJsonExpression<TElement, bool>? predicate
) : IJsonExpressionLinqWhere<TIn, TOut>
{
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (source is null || predicate is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Where: Source or Predicate is null"));
            result = null;
            return false;
        }

        if (!source.TryCompile(parameter, out var sourceExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Where: Failed to compile Source or Predicate"));
            result = null;
            return false;
        }

        var itemParam = Expression.Parameter(typeof(TElement), "item");
        if (!predicate.TryCompile(itemParam, out var predicateBody))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Where: Failed to compile Source or Predicate"));
            result = null;
            return false;
        }

        var predicateFunc = Expression.Lambda<Func<TElement, bool>>(predicateBody, itemParam);
        
        // Call Enumerable.Where(source, predicate).ToArray()
        var whereMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Where) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TElement));
        var whereExpr = Expression.Call(whereMethod, sourceExpr, predicateFunc);
        
        var toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!
            .MakeGenericMethod(typeof(TElement));
        
        result = Expression.Call(toArrayMethod, whereExpr);
        return true;
    }
}
