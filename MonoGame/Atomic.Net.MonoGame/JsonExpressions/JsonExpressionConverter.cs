using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Base JSON converter for IJsonExpression types.
/// Handles deserialization of JSONLogic rules into typed expression instances.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public class JsonExpressionConverter<TIn, TOut> : JsonConverter<IJsonExpression<TIn, TOut>>
{
    /// <summary>
    /// Reads and converts JSON to an IJsonExpression instance.
    /// </summary>
    public override IJsonExpression<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // Parse the JSON to determine the operator
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Handle literals without operator keys
        if (root.ValueKind == JsonValueKind.Number)
        {
            var value = root.GetSingle();
            return new JsonExpressionNumberLiteral<TIn, TOut>(value);
        }

        if (root.ValueKind == JsonValueKind.String)
        {
            var value = root.GetString();
            return new JsonExpressionStringLiteral<TIn, TOut>(value);
        }

        if (root.ValueKind == JsonValueKind.True || root.ValueKind == JsonValueKind.False)
        {
            var value = root.GetBoolean();
            return new JsonExpressionBoolLiteral<TIn, TOut>(value);
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            return JsonSerializer.Deserialize<IJsonExpressionArrayLiteral<TIn, TOut>>(root, options);
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException(
                $"Expected: Object with operator key, Actual: {root.ValueKind}"
            );
        }

        // Find the operator key
        var enumerator = root.EnumerateObject();
        if (!enumerator.MoveNext())
        {
            throw new JsonException("Expected: Object with at least one property (operator key)");
        }

        var operatorProperty = enumerator.Current;
        var operatorKey = operatorProperty.Name;
        var operatorValue = operatorProperty.Value;

        // Map operator key to expression type and deserialize the operator's VALUE
        return operatorKey switch
        {
            // Comparison operators
            ">" => JsonSerializer.Deserialize<IJsonExpressionSymbolGreaterThan<TIn, TOut>>(operatorValue, options),
            "<" => JsonSerializer.Deserialize<IJsonExpressionSymbolLessThan<TIn, TOut>>(operatorValue, options),
            "==" => JsonSerializer.Deserialize<IJsonExpressionSymbolEquals<TIn, TOut>>(operatorValue, options),
            "!=" => JsonSerializer.Deserialize<IJsonExpressionSymbolNotEquals<TIn, TOut>>(operatorValue, options),
            ">=" => JsonSerializer.Deserialize<IJsonExpressionSymbolGreaterThanOrEqual<TIn, TOut>>(operatorValue, options),
            "<=" => JsonSerializer.Deserialize<IJsonExpressionSymbolLessThanOrEqual<TIn, TOut>>(operatorValue, options),
            
            // Logical operators
            "and" => JsonSerializer.Deserialize<IJsonExpressionAnd<TIn, TOut>>(operatorValue, options),
            "or" => JsonSerializer.Deserialize<IJsonExpressionOr<TIn, TOut>>(operatorValue, options),
            "!" => JsonSerializer.Deserialize<IJsonExpressionSymbolNot<TIn, TOut>>(operatorValue, options),
            
            // Math operators
            "-" => JsonSerializer.Deserialize<IJsonExpressionSymbolSubtract<TIn, TOut>>(operatorValue, options),
            "*" => JsonSerializer.Deserialize<IJsonExpressionSymbolMultiply<TIn, TOut>>(operatorValue, options),
            "/" => JsonSerializer.Deserialize<IJsonExpressionSymbolDivide<TIn, TOut>>(operatorValue, options),
            "%" => JsonSerializer.Deserialize<IJsonExpressionSymbolModulo<TIn, TOut>>(operatorValue, options),
            "+" => JsonSerializer.Deserialize<IJsonExpressionSymbolAdd<TIn, TOut>>(operatorValue, options),
            
            // Control flow
            "if" => JsonSerializer.Deserialize<IJsonExpressionIf<TIn, TOut>>(operatorValue, options),
            "var" => JsonSerializer.Deserialize<IJsonExpressionVar<TIn, TOut>>(operatorValue, options),
            
            // Array/Math operations
            "max" => JsonSerializer.Deserialize<IJsonExpressionMax<TIn, TOut>>(operatorValue, options),
            "min" => JsonSerializer.Deserialize<IJsonExpressionMin<TIn, TOut>>(operatorValue, options),
            "addRange" => JsonSerializer.Deserialize<IJsonExpressionLinqAddRange<TIn, TOut>>(operatorValue, options),
            "contains" => JsonSerializer.Deserialize<IJsonExpressionContains<TIn, TOut>>(operatorValue, options),

            // LINQ operators
            "all" => JsonSerializer.Deserialize<IJsonExpressionLinqAll<TIn, TOut>>(operatorValue, options),
            "any" => JsonSerializer.Deserialize<IJsonExpressionLinqAny<TIn, TOut>>(operatorValue, options),
            "none" => JsonSerializer.Deserialize<IJsonExpressionLinqNone<TIn, TOut>>(operatorValue, options),
            "where" => JsonSerializer.Deserialize<IJsonExpressionLinqWhere<TIn, TOut>>(operatorValue, options),
            "select" => JsonSerializer.Deserialize<IJsonExpressionLinqSelect<TIn, TOut>>(operatorValue, options),
            "selectMany" => JsonSerializer.Deserialize<IJsonExpressionLinqSelectMany<TIn, TOut>>(operatorValue, options),
            "aggregate" => JsonSerializer.Deserialize<IJsonExpressionLinqAggregate<TIn, TOut>>(operatorValue, options),
            "add" => JsonSerializer.Deserialize<IJsonExpressionLinqAdd<TIn, TOut>>(operatorValue, options),
            
            // String operations
            "stringContains" => JsonSerializer.Deserialize<IJsonExpressionStringContains<TIn, TOut>>(operatorValue, options),
            "substring" => JsonSerializer.Deserialize<IJsonExpressionSubstring<TIn, TOut>>(operatorValue, options),
            
            // Logging
            "log" => JsonSerializer.Deserialize<IJsonExpressionLog<TIn, TOut>>(operatorValue, options),
            
            _ => throw new JsonException($"Unknown operator: {operatorKey}")
        };
    }

    /// <summary>
    /// Writes an IJsonExpression instance to JSON.
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpression<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
