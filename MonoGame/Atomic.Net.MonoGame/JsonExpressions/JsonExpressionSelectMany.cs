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
/// <typeparam name="TSource">Source array element type</typeparam>
/// <typeparam name="TResult">Result element type after flattening</typeparam>
[JsonConverter(typeof(JsonExpressionSelectManyConverterFactory))]
public sealed class JsonExpressionSelectMany<TIn, TSource, TResult>(
    JsonExpression<TIn, TSource[]>? source,
    JsonExpression<TSource, TResult[]>? selector
) : JsonExpression<TIn, TResult[]>
{
    /// <summary>
    /// Source array expression.
    /// </summary>
    public JsonExpression<TIn, TSource[]>? Source { get; } = source;

    /// <summary>
    /// Selector expression (maps each element to result array, then flattens).
    /// </summary>
    public JsonExpression<TSource, TResult[]>? Selector { get; } = selector;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TResult[]>>? result
    )
    {
        if (Source is null || Selector is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("SelectMany: Source or Selector is null"));
            result = null;
            return false;
        }

        if (!Source.TryCompile(out var sourceFunc) || !Selector.TryCompile(out var selectorFunc))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("SelectMany: Failed to compile Source or Selector"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var sourceExpr = Expression.Invoke(sourceFunc, parameter);
        
        // Call Enumerable.SelectMany(source, selector).ToArray()
        var selectManyMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.SelectMany) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TSource), typeof(TResult));
        var selectManyExpr = Expression.Call(selectManyMethod, sourceExpr, selectorFunc);
        
        var toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!
            .MakeGenericMethod(typeof(TResult));
        var arrayExpr = Expression.Call(toArrayMethod, selectManyExpr);

        result = Expression.Lambda<Func<TIn, TResult[]>>(arrayExpr, parameter);
        return true;
    }
}
