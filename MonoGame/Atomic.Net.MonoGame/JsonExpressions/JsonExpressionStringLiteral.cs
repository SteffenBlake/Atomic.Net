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
public sealed class JsonExpressionStringLiteral<TIn, TOut>(
    string? value
) : IJsonExpressionStringLiteral<TIn, TOut>
{
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (value is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("StringLiteral: Value is null"));
            result = null;
            return false;
        }

        // Convert string to TOut type
        var converted = (TOut)(object)value;
        result = Expression.Constant(converted, typeof(TOut));
        return true;
    }
}
