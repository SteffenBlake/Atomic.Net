using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Interface for less-than-or-equal comparison expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by this expression</typeparam>
[JsonConverter(typeof(JsonExpressionSymbolLessThanOrEqualConverterFactory))]
public interface IJsonExpressionSymbolLessThanOrEqual<TIn, TOut> : IJsonExpression<TIn, TOut>
{
}
