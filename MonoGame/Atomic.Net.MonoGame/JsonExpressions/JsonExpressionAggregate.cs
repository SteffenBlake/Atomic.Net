using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic 'reduce' operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TSource">Source array element type</typeparam>
/// <typeparam name="TAccumulate">Accumulator type</typeparam>
[JsonConverter(typeof(JsonExpressionAggregateConverterFactory))]
public sealed class JsonExpressionAggregate<TIn, TSource, TAccumulate> : JsonExpression<TIn, TAccumulate>
{
    public override bool TryCompile(
        JsonDocument rule,
        [NotNullWhen(true)]
        out Expression<Func<TIn, TAccumulate>>? result
    )
    {
        throw new NotImplementedException();
    }
}
