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
    IJsonExpression<TIn, string>? haystack,
    IJsonExpression<TIn, string>? needle
) : IJsonExpressionContains<TIn, TOut>
{
    /// <summary>
    /// String to search in (haystack).
    /// </summary>
    public IJsonExpression<TIn, string>? Haystack { get; } = haystack;

    /// <summary>
    /// String to search for (needle).
    /// </summary>
    public IJsonExpression<TIn, string>? Needle { get; } = needle;

    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (Haystack is null || Needle is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Contains: Haystack or Needle is null"));
            result = null;
            return false;
        }

        if (!Haystack.TryCompile(parameter, out var haystackExpr) || !Needle.TryCompile(parameter, out var needleExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Contains: Failed to compile Haystack or Needle"));
            result = null;
            return false;
        }

        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        
        result = Expression.Call(haystackExpr, containsMethod, needleExpr);
        return true;
    }
}
