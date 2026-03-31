using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic boolean literal expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Requested output type</typeparam>
[JsonConverter(typeof(JsonExpressionBoolLiteralConverterFactory))]
public sealed class JsonExpressionBoolLiteral<TIn, TOut>(bool value) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// The literal boolean value.
    /// </summary>
    public bool Value { get; } = value;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        // Convert bool to TOut type
        var converted = (TOut)(object)Value;
        var constant = Expression.Constant(converted, typeof(TOut));
        var parameter = Expression.Parameter(typeof(TIn), "input");
        result = Expression.Lambda<Func<TIn, TOut>>(constant, parameter);
        return true;
    }
}
