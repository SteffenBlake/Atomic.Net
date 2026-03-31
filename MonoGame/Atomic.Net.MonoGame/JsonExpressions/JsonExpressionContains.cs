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
    IJsonExpression<TIn, string>? substring
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

        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        
        result = Expression.Call(textExpr, containsMethod, substringExpr);
        return true;
    }
}
