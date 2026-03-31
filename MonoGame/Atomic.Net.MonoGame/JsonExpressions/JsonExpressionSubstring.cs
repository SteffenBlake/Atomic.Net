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
public sealed class JsonExpressionSubstring<TIn, TOut>(
    IJsonExpression<TIn, string>? @string,
    IJsonExpression<TIn, int>? start,
    IJsonExpression<TIn, int>? length
) : IJsonExpressionSubstring<TIn, TOut>
{
    /// <summary>
    /// String expression to extract substring from.
    /// </summary>
    public IJsonExpression<TIn, string>? String { get; } = @string;

    /// <summary>
    /// Start index expression.
    /// </summary>
    public IJsonExpression<TIn, int>? Start { get; } = start;

    /// <summary>
    /// Optional length expression.
    /// </summary>
    public IJsonExpression<TIn, int>? Length { get; } = length;

    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (String is null || Start is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Substring: String or Start is null"));
            result = null;
            return false;
        }

        if (!String.TryCompile(parameter, out var stringExpr) || !Start.TryCompile(parameter, out var startExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Substring: Failed to compile String or Start"));
            result = null;
            return false;
        }

        Expression substringExpr;
        
        if (Length is not null && Length.TryCompile(parameter, out var lengthExpr))
        {
            // Use string.Substring(int startIndex, int length)
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

        result = substringExpr;
        return true;
    }
}
