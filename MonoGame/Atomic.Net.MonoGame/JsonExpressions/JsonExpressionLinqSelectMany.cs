using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic selectMany operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Result element type after flattening</typeparam>
/// <typeparam name="TInner">Source array element type</typeparam>
public sealed class JsonExpressionLinqSelectMany<TIn, TOut, TInner>(
    IJsonExpression<TIn, TInner[]>? source,
    IJsonExpression<TInner, TOut[]>? selector
) : IJsonExpressionLinqSelectMany<TIn, TOut[]>
{
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (source is null || selector is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("SelectMany: Source or Selector is null"));
            result = null;
            return false;
        }

        if (!source.TryCompile(parameter, out var sourceExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("SelectMany: Failed to compile Source or Selector"));
            result = null;
            return false;
        }

        var itemParam = Expression.Parameter(typeof(TInner), "item");
        if (!selector.TryCompile(itemParam, out var selectorBody))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("SelectMany: Failed to compile Source or Selector"));
            result = null;
            return false;
        }

        var selectorFunc = Expression.Lambda<Func<TInner, TOut[]>>(selectorBody, itemParam);
        
        // Call Enumerable.SelectMany(source, selector).ToArray()
        var selectManyMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.SelectMany) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TInner), typeof(TOut));
        var selectManyExpr = Expression.Call(selectManyMethod, sourceExpr, selectorFunc);
        
        var toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!
            .MakeGenericMethod(typeof(TOut));
        
        result = Expression.Call(toArrayMethod, selectManyExpr);
        return true;
    }
}
