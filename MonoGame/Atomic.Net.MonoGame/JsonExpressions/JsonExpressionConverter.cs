using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Base JSON converter for JsonExpression types.
/// Handles deserialization of JSONLogic rules into typed expression instances.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public class JsonExpressionConverter<TIn, TOut> : JsonConverter<JsonExpression<TIn, TOut>>
{
    /// <summary>
    /// Reads and converts JSON to a JsonExpression instance.
    /// </summary>
    public override JsonExpression<TIn, TOut>? Read(
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
            return JsonSerializer.Deserialize<JsonExpressionArrayLiteral<TIn, TOut>>(root, options);
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
            ">" => JsonSerializer.Deserialize<JsonExpressionGreaterThan<TIn, TOut>>(operatorValue, options),
            "<" => JsonSerializer.Deserialize<JsonExpressionLessThan<TIn, TOut>>(operatorValue, options),
            "==" => JsonSerializer.Deserialize<JsonExpressionEquals<TIn, TOut>>(operatorValue, options),
            "!=" => JsonSerializer.Deserialize<JsonExpressionNotEquals<TIn, TOut>>(operatorValue, options),
            ">=" => JsonSerializer.Deserialize<JsonExpressionGreaterThanOrEqual<TIn, TOut>>(operatorValue, options),
            "<=" => JsonSerializer.Deserialize<JsonExpressionLessThanOrEqual<TIn, TOut>>(operatorValue, options),
            
            // Logical operators
            "and" => JsonSerializer.Deserialize<JsonExpressionAnd<TIn, TOut>>(operatorValue, options),
            "or" => JsonSerializer.Deserialize<JsonExpressionOr<TIn, TOut>>(operatorValue, options),
            "!" => JsonSerializer.Deserialize<JsonExpressionNot<TIn, TOut>>(operatorValue, options),
            
            // Math operators
            "-" => JsonSerializer.Deserialize<JsonExpressionSubtract<TIn, TOut>>(operatorValue, options),
            "*" => JsonSerializer.Deserialize<JsonExpressionMultiply<TIn, TOut>>(operatorValue, options),
            "/" => JsonSerializer.Deserialize<JsonExpressionDivide<TIn, TOut>>(operatorValue, options),
            "%" => JsonSerializer.Deserialize<JsonExpressionModulo<TIn, TOut>>(operatorValue, options),
            "+" => JsonSerializer.Deserialize<JsonExpressionSymbolAdd<TIn, TOut>>(operatorValue, options),
            
            // Control flow
            "if" => JsonSerializer.Deserialize<JsonExpressionIf<TIn, TOut>>(operatorValue, options),
            "var" => JsonSerializer.Deserialize<JsonExpressionVar<TIn, TOut>>(operatorValue, options),
            
            // Array/Math operations
            "max" => JsonSerializer.Deserialize<JsonExpressionMax<TIn, TOut>>(operatorValue, options),
            "min" => JsonSerializer.Deserialize<JsonExpressionMin<TIn, TOut>>(operatorValue, options),
            "append" => JsonSerializer.Deserialize<JsonExpressionAppend<TIn, TOut>>(operatorValue, options),
            "contains" => JsonSerializer.Deserialize<JsonExpressionContains<TIn, TOut>>(operatorValue, options),
            
            // String operations
            "stringContains" => JsonSerializer.Deserialize<JsonExpressionStringContains<TIn, TOut>>(operatorValue, options),
            "substring" => JsonSerializer.Deserialize<JsonExpressionSubstring<TIn, TOut>>(operatorValue, options),
            
            // Logging
            "log" => JsonSerializer.Deserialize<JsonExpressionLog<TIn, TOut>>(operatorValue, options),
            
            _ => throw new JsonException($"Unknown operator: {operatorKey}")
        };
    }

    /// <summary>
    /// Writes a JsonExpression instance to JSON.
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        JsonExpression<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
