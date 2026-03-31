using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by this expression</typeparam>
public sealed class JsonExpressionVar<TIn, TOut>(
    string[]? properties,
    int? arrayIndex,
    IJsonExpression<TIn, TOut>? defaultValue
) : IJsonExpressionVar<TIn, TOut>
{
    /// <summary>
    /// Pre-split property path segments (e.g., ["Address", "City"]), or null for identity.
    /// Parsed at load time by the converter; no string splitting occurs at compile time.
    /// </summary>
    public string[]? Properties { get; } = properties;

    /// <summary>
    /// Numeric array index for array-element access (e.g., {"var": 1}).
    /// Mutually exclusive with Properties.
    /// </summary>
    public int? ArrayIndex { get; } = arrayIndex;

    /// <summary>
    /// Optional default value expression used when the property path cannot be resolved.
    /// </summary>
    public IJsonExpression<TIn, TOut>? DefaultValue { get; } = defaultValue;

    public bool TryCompile(
        ParameterExpression parameter,
        [NotNullWhen(true)]
        out Expression? result
    )
    {
        // Numeric array index: {"var": 1} → input[1]
        if (ArrayIndex is not null)
        {
            var indexExpr = Expression.ArrayIndex(parameter, Expression.Constant(ArrayIndex.Value));
            result = indexExpr.Type != typeof(TOut)
                ? Expression.Convert(indexExpr, typeof(TOut))
                : indexExpr;
            return true;
        }

        // Null properties = identity: {"var": ""} → return parameter itself
        if (Properties is null)
        {
            result = parameter.Type != typeof(TOut)
                ? Expression.Convert(parameter, typeof(TOut))
                : parameter;
            return true;
        }

        // Navigate pre-split property chain — no reflection here, all resolved at load time
        Expression propertyExpr = parameter;

        foreach (var propertyName in Properties)
        {
            var propertyInfo = propertyExpr.Type.GetProperty(propertyName);
            if (propertyInfo is null)
            {
                // Property not found at compile time: fall back to default if one exists
                if (DefaultValue is not null)
                {
                    return DefaultValue.TryCompile(parameter, out result);
                }

                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Var: Property '{propertyName}' not found on type {propertyExpr.Type.Name}"
                ));
                result = null;
                return false;
            }

            propertyExpr = Expression.Property(propertyExpr, propertyInfo);
        }

        // Convert to TOut if needed, then null-coalesce with default if provided
        if (propertyExpr.Type != typeof(TOut))
        {
            propertyExpr = Expression.Convert(propertyExpr, typeof(TOut));
        }

        if (DefaultValue is not null)
        {
            if (!DefaultValue.TryCompile(parameter, out var defaultExpr))
            {
                result = null;
                return false;
            }
            // Null-coalesce: return property value if non-null, else default
            result = Expression.Coalesce(propertyExpr, defaultExpr);
        }
        else
        {
            result = propertyExpr;
        }

        return true;
    }
}
