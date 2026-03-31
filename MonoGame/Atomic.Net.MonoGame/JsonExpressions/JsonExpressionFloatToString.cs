using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Wraps a float-producing expression and converts its output to string via ToString().
/// Used by the '+' operator when mixing float operands inside a string concatenation.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
public sealed class JsonExpressionFloatToString<TIn>(
    IJsonExpression<TIn, float> inner
) : IJsonExpression<TIn, string>
{
    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        if (!inner.TryCompile(parameter, out var innerExpr))
        {
            result = null;
            return false;
        }

        var toStringMethod = typeof(float).GetMethod(nameof(float.ToString), Type.EmptyTypes)!;
        result = Expression.Call(innerExpr, toStringMethod);
        return true;
    }
}
