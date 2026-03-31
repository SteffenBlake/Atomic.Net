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
public sealed class JsonExpressionBoolLiteral<TIn, TOut>(
    bool value
) : IJsonExpressionBoolLiteral<TIn, TOut>
{
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        // Convert bool to TOut type
        var converted = (TOut)(object)value;
        result = Expression.Constant(converted, typeof(TOut));
        return true;
    }
}
