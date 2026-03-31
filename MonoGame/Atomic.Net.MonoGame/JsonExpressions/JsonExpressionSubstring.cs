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
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (@string is null || start is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Substring: String or Start is null"));
            result = null;
            return false;
        }

        if (!@string.TryCompile(parameter, out var stringExpr) || !start.TryCompile(parameter, out var startFloatExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Substring: Failed to compile String or Start"));
            result = null;
            return false;
        }

        var startExpr = Expression.Convert(startFloatExpr, typeof(int));
        Expression substringExpr;
        
        if (length is not null && length.TryCompile(parameter, out var lengthFloatExpr))
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
