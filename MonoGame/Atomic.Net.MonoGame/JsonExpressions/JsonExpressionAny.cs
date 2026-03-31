using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic any operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TSource">Array element type</typeparam>
[JsonConverter(typeof(JsonExpressionAnyConverterFactory))]
public sealed class JsonExpressionAny<TIn, TSource>(
    JsonExpression<TIn, TSource[]>? source,
    JsonExpression<TSource, bool>? predicate
) : JsonExpression<TIn, bool>
{
    /// <summary>
    /// Source array expression.
    /// </summary>
    public JsonExpression<TIn, TSource[]>? Source { get; } = source;

    /// <summary>
    /// Predicate expression (tests if any element matches).
    /// </summary>
    public JsonExpression<TSource, bool>? Predicate { get; } = predicate;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, bool>>? result
    )
    {
        if (Source is null || Predicate is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Any: Source or Predicate is null"));
            result = null;
            return false;
        }

        if (!Source.TryCompile(out var sourceFunc) || !Predicate.TryCompile(out var predicateFunc))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Any: Failed to compile Source or Predicate"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var sourceExpr = Expression.Invoke(sourceFunc, parameter);
        
        // Call Enumerable.Any(source, predicate)
        var anyMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TSource));
        var anyExpr = Expression.Call(anyMethod, sourceExpr, predicateFunc);

        result = Expression.Lambda<Func<TIn, bool>>(anyExpr, parameter);
        return true;
    }
}
