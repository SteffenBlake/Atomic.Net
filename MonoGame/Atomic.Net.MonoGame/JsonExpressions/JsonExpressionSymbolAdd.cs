using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by this expression</typeparam>
[JsonConverter(typeof(JsonExpressionSymbolAddConverterFactory))]
public sealed class JsonExpressionSymbolAdd<TIn, TOut> : JsonExpression<TIn, TOut>
{
    public override bool TryCompile(
        JsonDocument rule,
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        throw new NotImplementedException();
    }
}
