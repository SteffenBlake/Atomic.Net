using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSONLogic operator expression.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by this expression</typeparam>
[JsonConverter(typeof(JsonExpressionVarConverterFactory))]
public sealed class JsonExpressionVar<TIn, TOut>(
    string? path,
    JsonExpression<TIn, TOut>? defaultValue
) : JsonExpression<TIn, TOut>
{
    /// <summary>
    /// Property path to access (e.g., "Name" or "Address.City").
    /// </summary>
    public string? Path { get; } = path;

    /// <summary>
    /// Optional default value expression if path doesn't exist.
    /// </summary>
    public JsonExpression<TIn, TOut>? DefaultValue { get; } = defaultValue;

    public override bool TryCompile(
        [NotNullWhen(true)]
        out Expression<Func<TIn, TOut>>? result
    )
    {
        if (string.IsNullOrEmpty(Path))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Var: Path is null or empty"));
            result = null;
            return false;
        }

        var parameter = Expression.Parameter(typeof(TIn), "input");
        
        // Navigate nested property path (e.g., "Address.City")
        var properties = Path.Split('.');
        Expression propertyExpr = parameter;
        
        foreach (var propertyName in properties)
        {
            var propertyInfo = propertyExpr.Type.GetProperty(propertyName);
            if (propertyInfo is null)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent($"Var: Property '{propertyName}' not found on type {propertyExpr.Type.Name}"));
                result = null;
                return false;
            }
            propertyExpr = Expression.Property(propertyExpr, propertyInfo);
        }
        
        // Convert to TOut if needed
        if (propertyExpr.Type != typeof(TOut))
        {
            propertyExpr = Expression.Convert(propertyExpr, typeof(TOut));
        }

        result = Expression.Lambda<Func<TIn, TOut>>(propertyExpr, parameter);
        return true;
    }
}
