using System.Text.Json;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Extension methods for JsonElement to infer expression types.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    /// Infers the output type of a JsonExpression from its JSON structure.
    /// Recursively analyzes the JSON to determine the actual type produced by the expression.
    /// Validates that the inferred type is supported (float, string, bool, or arrays of these).
    /// </summary>
    /// <typeparam name="TIn">The input type for inferring 'var' operators</typeparam>
    /// <param name="element">JsonElement representing the expression</param>
    /// <returns>The inferred output type</returns>
    /// <exception cref="JsonException">When type cannot be inferred or is not supported</exception>
    public static Type InferJsonExpressionType<TIn>(this JsonElement element)
    {
        var inferredType = InferJsonExpressionTypeInternal<TIn>(element);
        
        if (!IsTypeSupportedByJsonExpression(inferredType))
        {
            throw new JsonException(
                $"Inferred type '{inferredType.Name}' is not supported by JsonExpression. " +
                $"Supported types: float, string, bool, or arrays of these types."
            );
        }
        
        return inferredType;
    }

    /// <summary>
    /// Checks if a type is supported by JsonExpression system.
    /// </summary>
    private static bool IsTypeSupportedByJsonExpression(Type type)
    {
        // Base types
        if (type == typeof(float) || type == typeof(string) || type == typeof(bool))
        {
            return true;
        }
        
        // Array types
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            if (elementType is not null)
            {
                return IsTypeSupportedByJsonExpression(elementType);
            }
        }
        
        return false;
    }

    /// <summary>
    /// Internal implementation that infers the output type without validation.
    /// </summary>
    private static Type InferJsonExpressionTypeInternal<TIn>(this JsonElement element)
    {
        // Literals have direct type mappings
        if (element.ValueKind == JsonValueKind.Number)
        {
            return typeof(float);
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return typeof(string);
        }

        if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
        {
            return typeof(bool);
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            // Array literal - infer element type from first element
            var arrayLength = element.GetArrayLength();
            if (arrayLength == 0)
            {
                throw new JsonException("Cannot infer type from empty array literal");
            }

            var firstElementType = element[0].InferJsonExpressionTypeInternal<TIn>();
            return firstElementType.MakeArrayType();
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException(
                $"Expected: Object with operator key for type inference, Actual: {element.ValueKind}"
            );
        }

        // Find the operator key
        var enumerator = element.EnumerateObject();
        if (!enumerator.MoveNext())
        {
            throw new JsonException("Expected: Object with at least one property (operator key)");
        }

        var operatorProperty = enumerator.Current;
        var operatorKey = operatorProperty.Name;
        var operatorValue = operatorProperty.Value;

        // Map operator key to output type
        return operatorKey switch
        {
            // Comparison operators always return bool
            ">" => typeof(bool),
            "<" => typeof(bool),
            "==" => typeof(bool),
            "!=" => typeof(bool),
            ">=" => typeof(bool),
            "<=" => typeof(bool),
            
            // Logical operators always return bool
            "and" => typeof(bool),
            "or" => typeof(bool),
            "!" => typeof(bool),
            
            // LINQ filter/test operators return bool
            "all" => typeof(bool),
            "any" => typeof(bool),
            "none" => typeof(bool),
            "contains" => typeof(bool),
            
            // String operators
            "stringContains" => typeof(bool),
            "substring" => typeof(string),
            
            // Math operators - infer from first operand
            "+" => InferMathOperatorType<TIn>(operatorValue),
            "-" => InferMathOperatorType<TIn>(operatorValue),
            "*" => InferMathOperatorType<TIn>(operatorValue),
            "/" => InferMathOperatorType<TIn>(operatorValue),
            "%" => InferMathOperatorType<TIn>(operatorValue),
            "max" => InferMathOperatorType<TIn>(operatorValue),
            "min" => InferMathOperatorType<TIn>(operatorValue),
            
            // If operator - infer from then branch
            "if" => InferIfOperatorType<TIn>(operatorValue),
            
            // Var operator - infer from property path on TIn
            "var" => InferVarOperatorType<TIn>(operatorValue),
            
            // LINQ transformation operators - infer from selector/result type
            "select" => InferSelectOperatorType<TIn>(operatorValue),
            "selectMany" => InferSelectManyOperatorType<TIn>(operatorValue),
            "where" => InferWhereOperatorType<TIn>(operatorValue),
            
            // Array operators
            "append" => InferAppendOperatorType<TIn>(operatorValue),
            
            // Aggregate - infer from initial value
            "aggregate" => InferAggregateOperatorType<TIn>(operatorValue),
            
            // Log - passthrough type from value
            "log" => operatorValue.InferJsonExpressionTypeInternal<TIn>(),
            
            _ => throw new JsonException($"Cannot infer type for unknown operator: {operatorKey}")
        };
    }

    private static Type InferMathOperatorType<TIn>(JsonElement operatorValue)
    {
        // Math operators take an array of operands - infer from first operand
        if (operatorValue.ValueKind != JsonValueKind.Array || operatorValue.GetArrayLength() == 0)
        {
            throw new JsonException("Cannot infer type from empty operands array");
        }

        return operatorValue[0].InferJsonExpressionTypeInternal<TIn>();
    }

    private static Type InferIfOperatorType<TIn>(JsonElement operatorValue)
    {
        // If operator: [condition, thenBranch, elseBranch, ...]
        // Infer from then branch (index 1)
        if (operatorValue.ValueKind != JsonValueKind.Array || operatorValue.GetArrayLength() == 0)
        {
            throw new JsonException("Cannot infer type from empty if operator array");
        }

        return operatorValue[1].InferJsonExpressionTypeInternal<TIn>();
    }

    private static Type InferSelectOperatorType<TIn>(JsonElement operatorValue)
    {
        // Select: [source, selector]
        // Infer result element type from selector, then make array
        if (operatorValue.ValueKind != JsonValueKind.Array || operatorValue.GetArrayLength() == 0)
        {
            throw new JsonException("Cannot infer type from empty select operator array");
        }

        var selectorType = operatorValue[1].InferJsonExpressionTypeInternal<TIn>();
        return selectorType.MakeArrayType();
    }

    private static Type InferSelectManyOperatorType<TIn>(JsonElement operatorValue)
    {
        // SelectMany: [source, selector]
        // Selector returns array, so result is flattened array
        if (operatorValue.ValueKind != JsonValueKind.Array || operatorValue.GetArrayLength() == 0)
        {
            throw new JsonException("Cannot infer type from empty selectMany operator array");
        }

        var selectorType = operatorValue[1].InferJsonExpressionTypeInternal<TIn>();
        // Selector should return array, result is same array type
        return selectorType;
    }

    private static Type InferWhereOperatorType<TIn>(JsonElement operatorValue)
    {
        // Where: [source, predicate]
        // Returns same array type as source
        if (operatorValue.ValueKind != JsonValueKind.Array || operatorValue.GetArrayLength() == 0)
        {
            throw new JsonException("Cannot infer type from empty where operator array");
        }

        return operatorValue[0].InferJsonExpressionTypeInternal<TIn>();
    }

    private static Type InferAppendOperatorType<TIn>(JsonElement operatorValue)
    {
        // Append: [first, second]
        // Both should be arrays, return array type from first
        if (operatorValue.ValueKind != JsonValueKind.Array || operatorValue.GetArrayLength() == 0)
        {
            throw new JsonException("Cannot infer type from empty append operator array");
        }

        return operatorValue[0].InferJsonExpressionTypeInternal<TIn>();
    }

    private static Type InferAggregateOperatorType<TIn>(JsonElement operatorValue)
    {
        // Aggregate: [source, accumulator, initialValue]
        // Return type is same as initialValue
        if (operatorValue.ValueKind != JsonValueKind.Array || operatorValue.GetArrayLength() == 0)
        {
            throw new JsonException("Cannot infer type from empty aggregate operator array");
        }

        return operatorValue[2].InferJsonExpressionTypeInternal<TIn>();
    }

    private static Type InferVarOperatorType<TIn>(JsonElement operatorValue)
    {
        // Var can be:
        // - String: "PropertyName" or "Nested.Property"
        // - Array: ["PropertyName"] or ["PropertyName", defaultValue]
        // - Number: 1 (for array indexing)
        // - Empty string: "" (returns entire TIn)
        
        string? path = null;
        
        if (operatorValue.ValueKind == JsonValueKind.String)
        {
            path = operatorValue.GetString();
        }
        else if (operatorValue.ValueKind == JsonValueKind.Number)
        {
            // Array index - only valid for array types
            if (!typeof(TIn).IsArray)
            {
                throw new JsonException($"Cannot use numeric var index on non-array type {typeof(TIn).Name}");
            }
            return typeof(TIn).GetElementType() ?? throw new JsonException("Array element type is null");
        }
        else if (operatorValue.ValueKind == JsonValueKind.Array)
        {
            if (operatorValue.GetArrayLength() == 0)
            {
                throw new JsonException("Cannot infer type from empty var operator array");
            }
            
            var firstElement = operatorValue[0];
            if (firstElement.ValueKind != JsonValueKind.String)
            {
                throw new JsonException($"Expected string for var property path, got {firstElement.ValueKind}");
            }
            
            path = firstElement.GetString();
        }
        else
        {
            throw new JsonException($"Invalid var operator value kind: {operatorValue.ValueKind}");
        }
        
        // Empty string returns entire TIn
        if (string.IsNullOrEmpty(path))
        {
            return typeof(TIn);
        }
        
        // Navigate property path
        var properties = path.Split('.');
        var currentType = typeof(TIn);
        
        foreach (var propertyName in properties)
        {
            var propertyInfo = currentType.GetProperty(propertyName);
            if (propertyInfo is null)
            {
                throw new JsonException($"Property '{propertyName}' not found on type {currentType.Name}");
            }
            currentType = propertyInfo.PropertyType;
        }
        
        return currentType;
    }
}

