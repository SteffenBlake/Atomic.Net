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
public sealed class JsonExpressionBoolLiteral<TIn, TOut>(bool value) : IJsonExpressionBoolLiteral<TIn, TOut>
{
    /// <summary>
    /// The literal boolean value.
    /// </summary>
    public bool Value { get; } = value;

    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        // Convert bool to TOut type
        var converted = (TOut)(object)Value;
        result = Expression.Constant(converted, typeof(TOut));
        return true;
    }
}
