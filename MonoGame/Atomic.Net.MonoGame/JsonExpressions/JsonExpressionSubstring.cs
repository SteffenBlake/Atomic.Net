using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by this expression</typeparam>
public sealed class JsonExpressionSubstring<TIn, TOut>(
    IJsonExpression<TIn, string>? @string,
    IJsonExpression<TIn, float>? start,
    IJsonExpression<TIn, float>? length
) : IJsonExpressionSubstring<TIn, TOut>
{
    /// <summary>
    /// String expression to extract substring from.
    /// </summary>
    public IJsonExpression<TIn, string>? String { get; } = @string;

    /// <summary>
    /// Start index expression (stored as float, converted to int at compile time).
    /// </summary>
    public IJsonExpression<TIn, float>? Start { get; } = start;

    /// <summary>
    /// Optional length expression (stored as float, converted to int at compile time).
    /// </summary>
    public IJsonExpression<TIn, float>? Length { get; } = length;

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

        if (!String.TryCompile(parameter, out var stringExpr) || !Start.TryCompile(parameter, out var startFloatExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Substring: Failed to compile String or Start"));
            result = null;
            return false;
        }

        var startExpr = Expression.Convert(startFloatExpr, typeof(int));
        Expression substringExpr;
        
        if (Length is not null && Length.TryCompile(parameter, out var lengthFloatExpr))
        {
            var lengthExpr = Expression.Convert(lengthFloatExpr, typeof(int));
            var safeMethod = typeof(JsonExpressionSubstringHelpers)
                .GetMethod(nameof(JsonExpressionSubstringHelpers.SafeSubstringWithLength))!;
            substringExpr = Expression.Call(safeMethod, stringExpr, startExpr, lengthExpr);
        }
        else
        {
            var safeMethod = typeof(JsonExpressionSubstringHelpers)
                .GetMethod(nameof(JsonExpressionSubstringHelpers.SafeSubstringFromStart))!;
            substringExpr = Expression.Call(safeMethod, stringExpr, startExpr);
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

/// <summary>
/// Static helpers for safe substring operations with bounds clamping.
/// </summary>
internal static class JsonExpressionSubstringHelpers
{
    public static string SafeSubstringFromStart(string s, int start)
    {
        start = Math.Clamp(start, 0, s.Length);
        return s.Substring(start);
    }

    public static string SafeSubstringWithLength(string s, int start, int length)
    {
        start = Math.Clamp(start, 0, s.Length);
        length = Math.Clamp(length, 0, s.Length - start);
        return s.Substring(start, length);
    }
}
