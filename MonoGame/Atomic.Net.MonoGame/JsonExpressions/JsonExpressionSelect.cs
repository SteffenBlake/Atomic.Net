using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic 'map' operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TSource">Source array element type</typeparam>
/// <typeparam name="TResult">Result element type after transformation</typeparam>
[JsonConverter(typeof(JsonExpressionSelectConverterFactory))]
public sealed class JsonExpressionSelect<TIn, TSource, TResult> : JsonExpression<TIn, TResult[]>
{
    public override bool TryCompile(
        JsonDocument rule,
        [NotNullWhen(true)]
        out Expression<Func<TIn, TResult[]>>? result
    )
    {
        throw new NotImplementedException();
    }
}
