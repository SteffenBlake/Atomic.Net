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
[JsonConverter(typeof(JsonExpressionSubstringConverterFactory))]
public sealed class JsonExpressionSubstring<TIn, TOut>(
    JsonExpression<TIn, string>? @string,
    JsonExpression<TIn, int>? start,
    JsonExpression<TIn, int>? length
) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// String expression to extract substring from.
    /// </summary>
    public JsonExpression<TIn, string>? String { get; } = @string;

    /// <summary>
    /// Start index expression.
    /// </summary>
    public JsonExpression<TIn, int>? Start { get; } = start;

    /// <summary>
    /// Optional length expression.
    /// </summary>
    public JsonExpression<TIn, int>? Length { get; } = length;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        if (String is null || Start is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Substring: String or Start is null"));
            result = null;
            return false;
        }

        if (!String.TryCompile(out var stringFunc) || !Start.TryCompile(out var startFunc))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Substring: Failed to compile String or Start"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var stringExpr = Expression.Invoke(stringFunc, parameter);
        var startExpr = Expression.Invoke(startFunc, parameter);
        
        Expression substringExpr;
        
        if (Length is not null && Length.TryCompile(out var lengthFunc))
        {
            // Use string.Substring(int startIndex, int length)
            var lengthExpr = Expression.Invoke(lengthFunc, parameter);
            var substringMethod = typeof(string).GetMethod(nameof(string.Substring), [typeof(int), typeof(int)])!;
            substringExpr = Expression.Call(stringExpr, substringMethod, startExpr, lengthExpr);
        }
        else
        {
            // Use string.Substring(int startIndex)
            var substringMethod = typeof(string).GetMethod(nameof(string.Substring), [typeof(int)])!;
            substringExpr = Expression.Call(stringExpr, substringMethod, startExpr);
        }
        
        // Convert to TOut if needed
        if (substringExpr.Type != typeof(TOut))
        {
            substringExpr = Expression.Convert(substringExpr, typeof(TOut));
        }

        result = Expression.Lambda<Func<TIn, TOut>>(substringExpr, parameter);
        return true;
    }
}
