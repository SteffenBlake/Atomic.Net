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
public sealed class JsonExpressionNumberLiteral<TIn, TOut>(float value) : IJsonExpressionNumberLiteral<TIn, TOut>
{
    /// <summary>
    /// The literal numeric value.
    /// </summary>
    public float Value { get; } = value;

    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        // Convert number to TOut type
        var converted = Convert.ChangeType(Value, typeof(TOut));
        result = Expression.Constant(converted, typeof(TOut));
        return true;
    }
}
