using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionIf.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionIfConverter<TIn, TOut> : JsonConverter<JsonExpressionIf<TIn, TOut>>
{
    public override JsonExpressionIf<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array for if operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength < 3)
        {
            throw new JsonException(
                $"Expected: Array with at least 3 elements for if operator, Actual: Array with {arrayLength} elements"
            );
        }

        // Basic if-then-else: [condition, thenBranch, elseBranch]
        // Else-if chain: [condition1, value1, condition2, value2, ..., finalElse]
        // For simplicity, implement basic ternary (3 elements)
        
        if (arrayLength == 3)
        {
            // Simple ternary
            var condition = JsonSerializer.Deserialize<JsonExpression<TIn, bool>>(root[0], options);
            var thenBranch = JsonSerializer.Deserialize<JsonExpression<TIn, TOut>>(root[1], options);
            var elseBranch = JsonSerializer.Deserialize<JsonExpression<TIn, TOut>>(root[2], options);

            if (condition is null || thenBranch is null || elseBranch is null)
            {
                throw new JsonException(
                    $"Expected: Valid expressions for if condition, then, and else branches, Actual: One or more are null"
                );
            }

            return new JsonExpressionIf<TIn, TOut>(condition, thenBranch, elseBranch);
        }

        // Else-if chain: arrayLength > 3 and odd
        // [condition1, value1, condition2, value2, ..., finalElse]
        // Build nested if-else structure
        if (arrayLength % 2 == 0)
        {
            throw new JsonException(
                $"Expected: Odd number of elements for if-else chain, Actual: {arrayLength} elements"
            );
        }

        // Build from the end backwards
        var finalElse = JsonSerializer.Deserialize<JsonExpression<TIn, TOut>>(root[arrayLength - 1], options);
        if (finalElse is null)
        {
            throw new JsonException(
                $"Expected: Valid expression for final else branch, Actual: null"
            );
        }

        // Walk backwards through condition-value pairs
        var currentElse = finalElse;
        for (int i = arrayLength - 3; i >= 0; i -= 2)
        {
            var condition = JsonSerializer.Deserialize<JsonExpression<TIn, bool>>(root[i], options);
            var thenBranch = JsonSerializer.Deserialize<JsonExpression<TIn, TOut>>(root[i + 1], options);

            if (condition is null || thenBranch is null)
            {
                throw new JsonException(
                    $"Expected: Valid expressions for if condition and then branch at index {i}, Actual: One or both are null"
                );
            }

            // Nested if: if (condition) then thenBranch else previousElseBranch
            currentElse = new JsonExpressionIf<TIn, TOut>(condition, thenBranch, currentElse);
        }

        // The final currentElse is actually a complete if-else-if chain
        // But we need to return JsonExpressionIf, and we've built it as nested ifs
        // The outermost if is in currentElse at this point
        return currentElse as JsonExpressionIf<TIn, TOut>;
    }

    public override void Write(
        Utf8JsonWriter writer,
        JsonExpressionIf<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
