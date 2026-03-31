using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic number literal expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Requested output type</typeparam>
[JsonConverter(typeof(JsonExpressionNumberLiteralConverterFactory))]
public sealed class JsonExpressionNumberLiteral<TIn, TOut>(float value) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// The literal numeric value.
    /// </summary>
    public float Value { get; } = value;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        // Convert number to TOut type
        var converted = Convert.ChangeType(Value, typeof(TOut));
        var constant = Expression.Constant(converted, typeof(TOut));
        var parameter = Expression.Parameter(typeof(TIn), "input");
        result = Expression.Lambda<Func<TIn, TOut>>(constant, parameter);
        return true;
    }
}
