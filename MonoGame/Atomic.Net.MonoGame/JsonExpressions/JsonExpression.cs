using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Base class for strongly-typed JSONLogic expressions.
/// Compiles to Expression trees with zero boxing.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by this expression</typeparam>
[JsonConverter(typeof(JsonExpressionConverterFactory))]
public abstract class JsonExpression<TIn, TOut>
{
    /// <summary>
    /// Attempts to compile this expression to a strongly-typed LINQ expression tree.
    /// </summary>
    /// <param name="result">Compiled expression if successful</param>
    /// <returns>True if compilation succeeded and can produce TOut, false otherwise</returns>
    public abstract bool TryCompile(
        [NotNullWhen(true)] 
        out Expression<Func<TIn, TOut>>? result
    );
}
