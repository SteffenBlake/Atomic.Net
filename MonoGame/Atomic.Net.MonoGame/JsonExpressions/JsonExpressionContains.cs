using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by this expression</typeparam>
public sealed class JsonExpressionContains<TIn, TOut>(
    IJsonExpression<TIn, string>? text,
    IJsonExpression<TIn, string>? substring,
    IJsonExpression<TIn, bool>? caseSensitive = null
) : IJsonExpressionContains<TIn, TOut>
{
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (text is null || substring is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Contains: Text or Substring is null"));
            result = null;
            return false;
        }

        if (!text.TryCompile(parameter, out var textExpr) || !substring.TryCompile(parameter, out var substringExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Contains: Failed to compile Text or Substring"));
            result = null;
            return false;
        }

        // If caseSensitive not provided, default to case-sensitive (true)
        if (caseSensitive is null)
        {
            var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
            result = Expression.Call(textExpr, containsMethod, substringExpr);
            return true;
        }

        // Compile the case sensitivity expression
        if (!caseSensitive.TryCompile(parameter, out var caseSensitiveExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Contains: Failed to compile CaseSensitive"));
            result = null;
            return false;
        }

        // Get both string.Contains methods
        var containsMethodSimple = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        var containsMethodComparison = typeof(string).GetMethod(nameof(string.Contains), [typeof(string), typeof(StringComparison)])!;

        // Create expressions for both branches
        var caseSensitiveCall = Expression.Call(textExpr, containsMethodSimple, substringExpr);
        var caseInsensitiveCall = Expression.Call(
            textExpr,
            containsMethodComparison,
            substringExpr,
            Expression.Constant(StringComparison.OrdinalIgnoreCase)
        );

        // Use conditional: if (caseSensitive) then caseSensitiveCall else caseInsensitiveCall
        result = Expression.Condition(caseSensitiveExpr, caseSensitiveCall, caseInsensitiveCall);
        return true;
    }
}
