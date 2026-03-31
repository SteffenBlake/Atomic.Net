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
[JsonConverter(typeof(JsonExpressionStringContainsConverterFactory))]
public sealed class JsonExpressionStringContains<TIn, TOut>(
    JsonExpression<TIn, string>? haystack,
    JsonExpression<TIn, string>? needle
) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// String to search in (haystack).
    /// </summary>
    public JsonExpression<TIn, string>? Haystack { get; } = haystack;

    /// <summary>
    /// String to search for (needle).
    /// </summary>
    public JsonExpression<TIn, string>? Needle { get; } = needle;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        if (Haystack is null || Needle is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("StringContains: Haystack or Needle is null"));
            result = null;
            return false;
        }

        if (!Haystack.TryCompile(out var haystackFunc) || !Needle.TryCompile(out var needleFunc))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("StringContains: Failed to compile Haystack or Needle"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var haystackExpr = Expression.Invoke(haystackFunc, parameter);
        var needleExpr = Expression.Invoke(needleFunc, parameter);
        
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        var contains = Expression.Call(haystackExpr, containsMethod, needleExpr);
        
        result = Expression.Lambda<Func<TIn, TOut>>(contains, parameter);
        return true;
    }
}
