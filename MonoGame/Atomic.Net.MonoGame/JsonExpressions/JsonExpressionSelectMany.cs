using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic selectMany operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TSource">Source array element type</typeparam>
/// <typeparam name="TResult">Result element type after flattening</typeparam>
[JsonConverter(typeof(JsonExpressionSelectManyConverterFactory))]
public sealed class JsonExpressionSelectMany<TIn, TSource, TResult> : JsonExpression<TIn, TResult[]>
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
