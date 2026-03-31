using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Compiles JSONLogic expressions to strongly-typed C# Expression trees.
/// </summary>
public static class JsonExpressionCompiler
{
    /// <summary>
    /// Attempts to build a compiled expression from a JSONLogic rule.
    /// </summary>
    /// <typeparam name="TIn">Input data type</typeparam>
    /// <typeparam name="TOut">Expected output type</typeparam>
    /// <param name="rule">JSONLogic rule document</param>
    /// <param name="result">Compiled expression if successful</param>
    /// <returns>True if compilation succeeded, false otherwise</returns>
    public static bool TryBuild<TIn, TOut>(
        JsonDocument rule,
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        try
        {
            // Deserialize JSON to JsonExpression
            var expression = JsonSerializer.Deserialize<JsonExpression<TIn, TOut>>(
                rule.RootElement.GetRawText()
            );

            if (expression is null)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    "Failed to deserialize JSONLogic rule: result was null"
                ));

                result = null;
                return false;
            }

            // Compile expression to LINQ Expression tree
            return expression.TryCompile(out result);
        }
        catch (JsonException ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse JSONLogic rule: {ex.Message}"
            ));

            result = null;
            return false;
        }
    }
}
