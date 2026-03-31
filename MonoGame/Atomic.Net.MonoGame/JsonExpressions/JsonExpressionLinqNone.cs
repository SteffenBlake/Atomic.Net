using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic none operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by this expression</typeparam>
/// <typeparam name="TElement">Array element type</typeparam>
public sealed class JsonExpressionLinqNone<TIn, TOut, TElement>(
    IJsonExpression<TIn, TElement[]>? source,
    IJsonExpression<TElement, bool>? predicate
) : IJsonExpressionLinqNone<TIn, TOut>
{
    /// <summary>
    /// Source array expression.
    /// </summary>
    public IJsonExpression<TIn, TElement[]>? Source { get; } = source;

    /// <summary>
    /// Predicate expression (tests if no elements match).
    /// </summary>
    public IJsonExpression<TElement, bool>? Predicate { get; } = predicate;

    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (Source is null || Predicate is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("None: Source or Predicate is null"));
            result = null;
            return false;
        }

        if (!Source.TryCompile(parameter, out var sourceExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("None: Failed to compile Source or Predicate"));
            result = null;
            return false;
        }

        var itemParam = Expression.Parameter(typeof(TElement), "item");
        if (!Predicate.TryCompile(itemParam, out var predicateBody))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("None: Failed to compile Source or Predicate"));
            result = null;
            return false;
        }

        var predicateFunc = Expression.Lambda<Func<TElement, bool>>(predicateBody, itemParam);
        
        // Call !Enumerable.Any(source, predicate)
        var anyMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TElement));
        var anyExpr = Expression.Call(anyMethod, sourceExpr, predicateFunc);
        
        result = Expression.Not(anyExpr);
        return true;
    }
}
