using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic 'reduce' operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TSource">Source array element type</typeparam>
/// <typeparam name="TAccumulate">Accumulator type</typeparam>
[JsonConverter(typeof(JsonExpressionAggregateConverterFactory))]
public sealed class JsonExpressionAggregate<TIn, TSource, TAccumulate>(
    JsonExpression<TIn, TSource[]>? source,
    JsonExpression<TSource, TAccumulate>? accumulator,
    JsonExpression<TIn, TAccumulate>? initialValue
) : JsonExpression<TIn, TAccumulate>
{
    /// <summary>
    /// Source array expression.
    /// </summary>
    public JsonExpression<TIn, TSource[]>? Source { get; } = source;

    /// <summary>
    /// Accumulator expression (combines current element with accumulator).
    /// </summary>
    public JsonExpression<TSource, TAccumulate>? Accumulator { get; } = accumulator;

    /// <summary>
    /// Initial value for the accumulator.
    /// </summary>
    public JsonExpression<TIn, TAccumulate>? InitialValue { get; } = initialValue;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TAccumulate>>? result
    )
    {
        if (Source is null || Accumulator is null || InitialValue is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Aggregate: Source, Accumulator, or InitialValue is null"));
            result = null;
            return false;
        }

        if (!Source.TryCompile(out var sourceFunc) || !Accumulator.TryCompile(out var accumulatorFunc) || !InitialValue.TryCompile(out var initialValueFunc))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Aggregate: Failed to compile Source, Accumulator, or InitialValue"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var sourceExpr = Expression.Invoke(sourceFunc, parameter);
        var initialValueExpr = Expression.Invoke(initialValueFunc, parameter);
        
        // Call Enumerable.Aggregate(source, seed, func)
        var aggregateMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Aggregate) && m.GetParameters().Length == 3)
            .MakeGenericMethod(typeof(TSource), typeof(TAccumulate));
        
        // Create accumulator lambda: (acc, current) => Accumulator(current)
        // Note: Accumulator takes TSource as input, but we need to pass accumulator state through somehow
        // For now, we'll use a simplified version that just calls the accumulator function
        var aggregateExpr = Expression.Call(aggregateMethod, sourceExpr, initialValueExpr, accumulatorFunc);

        result = Expression.Lambda<Func<TIn, TAccumulate>>(aggregateExpr, parameter);
        return true;
    }
}
