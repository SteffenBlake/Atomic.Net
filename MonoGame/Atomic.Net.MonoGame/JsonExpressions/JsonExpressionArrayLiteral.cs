using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic array literal expression.
/// </summary>
/// <typeparam name="TIn">Input datatype</typeparam>
/// <typeparam name="TOut">Requested output type</typeparam>
[JsonConverter(typeof(JsonExpressionArrayLiteralConverterFactory))]
public sealed class JsonExpressionArrayLiteral<TIn, TOut>(JsonExpression<TIn, TOut>[]? value) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// The array elements (can be any mix of literals and expressions).
    /// </summary>
    public JsonExpression<TIn, TOut>[]? Value { get; } = value;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        if (Value is null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("ArrayLiteral: Value is null"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        var elementExpressions = new List<Expression>();

        // Compile each element expression
        foreach (var element in Value)
        {
            if (element is null || !element.TryCompile(out var elementFunc))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("ArrayLiteral: Element is null or failed to compile"));
                result = null;
                return false;
            }

            // Invoke the compiled element function with the input parameter
            var invocation = Expression.Invoke(elementFunc, parameter);
            elementExpressions.Add(invocation);
        }

        // Create array initialization expression
        var elementType = typeof(TOut).GetElementType() ?? typeof(object);
        var arrayInit = Expression.NewArrayInit(elementType, elementExpressions);
        
        result = Expression.Lambda<Func<TIn, TOut>>(arrayInit, parameter);
        return true;
    }
}
