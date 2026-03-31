using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Interface for strongly-typed JSONLogic expressions.
/// Compiles to Expression trees with zero boxing.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by this expression</typeparam>
[JsonConverter(typeof(JsonExpressionConverterFactory))]
public interface IJsonExpression<TIn, TOut>
{
    /// <summary>
    /// Attempts to compile this expression to a LINQ expression body (no lambda wrapper).
    /// </summary>
    /// <param name="parameter">The shared input parameter expression</param>
    /// <param name="result">Compiled expression body if successful</param>
    /// <returns>True if compilation succeeded and can produce TOut, false otherwise</returns>
    bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)] 
        out Expression? result
    );
}
