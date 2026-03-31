using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic none operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TSource">Array element type</typeparam>
[JsonConverter(typeof(JsonExpressionNoneConverterFactory))]
public sealed class JsonExpressionNone<TIn, TSource> : JsonExpression<TIn, bool>
{
    public override bool TryCompile(
        JsonDocument rule,
        [NotNullWhen(true)]
        out Expression<Func<TIn, bool>>? result
    )
    {
        throw new NotImplementedException();
    }
}
