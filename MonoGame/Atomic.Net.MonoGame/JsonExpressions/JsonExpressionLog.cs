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
[JsonConverter(typeof(JsonExpressionLogConverterFactory))]
public sealed class JsonExpressionLog<TIn, TOut>(JsonExpression<TIn, TOut>? value) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// Value to log and return.
    /// </summary>
    public JsonExpression<TIn, TOut>? Value { get; } = value;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        if (Value is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Log: Value is null"));
            result = null;
            return false;
        }

        if (!Value.TryCompile(out var valueFunc))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Log: Failed to compile Value"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var valueExpr = Expression.Invoke(valueFunc, parameter);
        
        // Log the value and return it
        var logMethod = typeof(JsonExpressionLog<TIn, TOut>).GetMethod(nameof(LogValue), 
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var log = Expression.Call(logMethod, valueExpr);
        
        result = Expression.Lambda<Func<TIn, TOut>>(log, parameter);
        return true;
    }

    private static T LogValue<T>(T value)
    {
        EventBus<LogEvent>.Push(new LogEvent(value?.ToString() ?? "null"));
        return value;
    }
}
