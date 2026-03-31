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
/// <typeparam name="TSource">Array element type</typeparam>
[JsonConverter(typeof(JsonExpressionWhereConverterFactory))]
public sealed class JsonExpressionWhere<TIn, TSource>(
    JsonExpression<TIn, TSource[]>? source,
    JsonExpression<TSource, bool>? predicate
) : JsonExpression<TIn, TSource[]>
{
    /// <summary>
    /// Source array expression.
    /// </summary>
    public JsonExpression<TIn, TSource[]>? Source { get; } = source;

    /// <summary>
    /// Predicate expression (tests which elements to include).
    /// </summary>
    public JsonExpression<TSource, bool>? Predicate { get; } = predicate;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TSource[]>>? result
    )
    {
        if (Source is null || Predicate is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Where: Source or Predicate is null"));
            result = null;
            return false;
        }

        if (!Source.TryCompile(out var sourceFunc) || !Predicate.TryCompile(out var predicateFunc))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Where: Failed to compile Source or Predicate"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var sourceExpr = Expression.Invoke(sourceFunc, parameter);
        
        // Call Enumerable.Where(source, predicate).ToArray()
        var whereMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Where) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TSource));
        var whereExpr = Expression.Call(whereMethod, sourceExpr, predicateFunc);
        
        var toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!
            .MakeGenericMethod(typeof(TSource));
        var arrayExpr = Expression.Call(toArrayMethod, whereExpr);

        result = Expression.Lambda<Func<TIn, TSource[]>>(arrayExpr, parameter);
        return true;
    }
}
