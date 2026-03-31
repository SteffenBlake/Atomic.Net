using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic 'map' operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Result element type after transformation</typeparam>
/// <typeparam name="TInner">Source array element type</typeparam>
public sealed class JsonExpressionLinqSelect<TIn, TOut, TInner>(
    IJsonExpression<TIn, TInner[]>? source,
    IJsonExpression<TInner, TOut>? selector
) : IJsonExpressionLinqSelect<TIn, TOut[]>
{
    /// <summary>
    /// Source array expression.
    /// </summary>
    public IJsonExpression<TIn, TInner[]>? Source { get; } = source;

    /// <summary>
    /// Selector expression (maps each element to result type).
    /// </summary>
    public IJsonExpression<TInner, TOut>? Selector { get; } = selector;

    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (Source is null || Selector is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Select: Source or Selector is null"));
            result = null;
            return false;
        }

        if (!Source.TryCompile(parameter, out var sourceExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Select: Failed to compile Source or Selector"));
            result = null;
            return false;
        }

        var itemParam = Expression.Parameter(typeof(TInner), "item");
        if (!Selector.TryCompile(itemParam, out var selectorBody))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Select: Failed to compile Source or Selector"));
            result = null;
            return false;
        }

        var selectorFunc = Expression.Lambda<Func<TInner, TOut>>(selectorBody, itemParam);
        
        // Call Enumerable.Select(source, selector).ToArray()
        var selectMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TInner), typeof(TOut));
        var selectExpr = Expression.Call(selectMethod, sourceExpr, selectorFunc);
        
        var toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!
            .MakeGenericMethod(typeof(TOut));
        
        result = Expression.Call(toArrayMethod, selectExpr);
        return true;
    }
}
