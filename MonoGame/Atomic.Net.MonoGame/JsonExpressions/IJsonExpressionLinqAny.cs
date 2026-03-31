using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Interface for any (LINQ Any) expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by this expression</typeparam>
[JsonConverter(typeof(JsonExpressionLinqAnyConverterFactory))]
public interface IJsonExpressionLinqAny<TIn, TOut> : IJsonExpression<TIn, TOut>
{
}
