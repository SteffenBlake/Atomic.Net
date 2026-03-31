using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Interface for LINQ Unshift expression (prepend item to array).
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by this expression</typeparam>
[JsonConverter(typeof(JsonExpressionLinqUnshiftConverterFactory))]
public interface IJsonExpressionLinqUnshift<TIn, TOut> : IJsonExpression<TIn, TOut>
{
}
