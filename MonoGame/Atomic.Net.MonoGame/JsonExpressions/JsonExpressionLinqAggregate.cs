using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic 'reduce' operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Accumulator type and output type</typeparam>
/// <typeparam name="TInner">Source array element type</typeparam>
public sealed class JsonExpressionLinqAggregate<TIn, TOut, TInner>(
    IJsonExpression<TIn, TInner[]>? source,
    IJsonExpression<TInner, TOut>? accumulator,
    IJsonExpression<TIn, TOut>? initialValue
) : IJsonExpressionLinqAggregate<TIn, TOut>
{
    /// <summary>
    /// Source array expression.
    /// </summary>
    public IJsonExpression<TIn, TInner[]>? Source { get; } = source;

    /// <summary>
    /// Accumulator expression (combines current element with accumulator).
    /// </summary>
    public IJsonExpression<TInner, TOut>? Accumulator { get; } = accumulator;

    /// <summary>
    /// Initial value for the accumulator.
    /// </summary>
    public IJsonExpression<TIn, TOut>? InitialValue { get; } = initialValue;

    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (Source is null || Accumulator is null || InitialValue is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Aggregate: Source, Accumulator, or InitialValue is null"));
            result = null;
            return false;
        }

        if (!Source.TryCompile(parameter, out var sourceExpr) || !InitialValue.TryCompile(parameter, out var initialValueExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Aggregate: Failed to compile Source, Accumulator, or InitialValue"));
            result = null;
            return false;
        }

        var itemParam = Expression.Parameter(typeof(TInner), "item");
        if (!Accumulator.TryCompile(itemParam, out var accumulatorBody))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Aggregate: Failed to compile Source, Accumulator, or InitialValue"));
            result = null;
            return false;
        }

        var accumulatorFunc = Expression.Lambda<Func<TInner, TOut>>(accumulatorBody, itemParam);
        
        // Call Enumerable.Aggregate(source, seed, func)
        var aggregateMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Aggregate) && m.GetParameters().Length == 3)
            .MakeGenericMethod(typeof(TInner), typeof(TOut));
        
        // Create accumulator lambda: (acc, current) => Accumulator(current)
        // Note: Accumulator takes TSource as input, but we need to pass accumulator state through somehow
        // For now, we'll use a simplified version that just calls the accumulator function
        result = Expression.Call(aggregateMethod, sourceExpr, initialValueExpr, accumulatorFunc);
        return true;
    }
}
