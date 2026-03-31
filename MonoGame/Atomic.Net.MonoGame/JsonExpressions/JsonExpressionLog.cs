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
public sealed class JsonExpressionLog<TIn, TOut>(IJsonExpression<TIn, TOut>? value) : IJsonExpressionLog<TIn, TOut>
{
    /// <summary>
    /// Value to log and return.
    /// </summary>
    public IJsonExpression<TIn, TOut>? Value { get; } = value;

    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (Value is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Log: Value is null"));
            result = null;
            return false;
        }

        if (!Value.TryCompile(parameter, out var valueExpr))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Log: Failed to compile Value"));
            result = null;
            return false;
        }
        
        // Log the value and return it
        var logMethod = typeof(JsonExpressionLog<TIn, TOut>).GetMethod(nameof(LogValue), 
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        result = Expression.Call(logMethod, valueExpr);
        return true;
    }

    private static T LogValue<T>(T value)
    {
        EventBus<LogEvent>.Push(new LogEvent(value?.ToString() ?? "null"));
        return value;
    }
}
