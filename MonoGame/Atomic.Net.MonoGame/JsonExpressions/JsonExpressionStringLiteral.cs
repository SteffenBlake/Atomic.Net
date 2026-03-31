using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic string literal expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Requested output type</typeparam>
[JsonConverter(typeof(JsonExpressionStringLiteralConverterFactory))]
public sealed class JsonExpressionStringLiteral<TIn, TOut>(string? value) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// The literal string value.
    /// </summary>
    public string? Value { get; } = value;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        if (Value is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("StringLiteral: Value is null"));
            result = null;
            return false;
        }

        // Convert string to TOut type
        var converted = (TOut)(object)Value;
        var constant = Expression.Constant(converted, typeof(TOut));
        var parameter = Expression.Parameter(typeof(TIn), "input");
        result = Expression.Lambda<Func<TIn, TOut>>(constant, parameter);
        return true;
    }
}
