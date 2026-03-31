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
    IJsonExpression<JsonAggregateContext<TInner, TOut>, TOut>? accumulator,
    IJsonExpression<TIn, TOut>? initialValue
) : IJsonExpressionLinqAggregate<TIn, TOut>
{
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (source is null || accumulator is null || initialValue is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Aggregate: Source, Accumulator, or InitialValue is null"));
            result = null;
            return false;
        }

        if (!source.TryCompile(parameter, out var sourceExpr) || !initialValue.TryCompile(parameter, out var initialValueExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Aggregate: Failed to compile Source or InitialValue"));
            result = null;
            return false;
        }

        // Compile the reducer to a delegate (load-time, not game-time)
        var ctxType = typeof(JsonAggregateContext<TInner, TOut>);
        var ctxParam = Expression.Parameter(ctxType, "ctx");
        if (!accumulator.TryCompile(ctxParam, out var accBody))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Aggregate: Failed to compile Accumulator"));
            result = null;
            return false;
        }

        var accDelegate = Expression.Lambda<Func<JsonAggregateContext<TInner, TOut>, TOut>>(accBody, ctxParam).Compile();

        // Build aggregate wrapper: (acc, current) => accDelegate(new Context(current, acc))
        var accParam = Expression.Parameter(typeof(TOut), "acc");
        var curParam = Expression.Parameter(typeof(TInner), "cur");
        var ctxCtor = ctxType.GetConstructors()[0];
        var ctxNew = Expression.New(ctxCtor, curParam, accParam);
        var delegateConst = Expression.Constant(accDelegate, typeof(Func<JsonAggregateContext<TInner, TOut>, TOut>));
        var callDelegate = Expression.Invoke(delegateConst, ctxNew);
        var wrapperLambda = Expression.Lambda<Func<TOut, TInner, TOut>>(callDelegate, accParam, curParam);

        // Enumerable.Aggregate<TInner, TOut>(source, seed, func)
        var aggregateMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Aggregate) && m.GetParameters().Length == 3)
            .MakeGenericMethod(typeof(TInner), typeof(TOut));

        result = Expression.Call(aggregateMethod, sourceExpr, initialValueExpr, wrapperLambda);
        return true;
    }
}
