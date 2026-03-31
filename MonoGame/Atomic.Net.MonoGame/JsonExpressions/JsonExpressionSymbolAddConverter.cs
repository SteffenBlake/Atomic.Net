using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionSymbolAdd.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionSymbolAddConverter<TIn, TOut> : JsonConverter<IJsonExpressionSymbolAdd<TIn, TOut>>
{
    public override IJsonExpressionSymbolAdd<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (typeof(TOut) == typeof(bool) || typeof(TOut).IsArray)
        {
            throw new JsonException(
                $"'+' operator requires a numeric or string TOut, got TOut={typeof(TOut).Name}"
            );
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Unary form: {"+": number} is identity/cast to number
        if (root.ValueKind == JsonValueKind.Number)
        {
            var operand = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(root, options);
            if (operand is null)
            {
                throw new JsonException("Expected: Valid expression for unary + operand, Actual: null");
            }
            return new JsonExpressionSymbolAdd<TIn, TOut>([operand]);
        }

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array for + operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength < 1)
        {
            throw new JsonException(
                $"Expected: Array with at least 1 element for + operator, Actual: Array with {arrayLength} elements"
            );
        }

        // When TOut=string, operands may be a mix of string and float.
        // Float operands are wrapped in JsonExpressionFloatToString.
        if (typeof(TOut) == typeof(string))
        {
            var stringOperands = new IJsonExpression<TIn, string>[arrayLength];
            for (var i = 0; i < arrayLength; i++)
            {
                var inferredType = root[i].InferJsonExpressionType<TIn>();
                if (inferredType == typeof(float))
                {
                    var floatOperand = JsonSerializer.Deserialize<IJsonExpression<TIn, float>>(root[i], options);
                    if (floatOperand is null)
                    {
                        throw new JsonException($"Expected: Valid float expression for + operand at index {i}, Actual: null");
                    }
                    stringOperands[i] = new JsonExpressionFloatToString<TIn>(floatOperand);
                }
                else
                {
                    var strOperand = JsonSerializer.Deserialize<IJsonExpression<TIn, string>>(root[i], options);
                    if (strOperand is null)
                    {
                        throw new JsonException($"Expected: Valid string expression for + operand at index {i}, Actual: null");
                    }
                    stringOperands[i] = strOperand;
                }
            }
            return new JsonExpressionSymbolAdd<TIn, TOut>((IJsonExpression<TIn, TOut>[])(object)stringOperands);
        }

        var operands = new IJsonExpression<TIn, TOut>[arrayLength];
        for (int i = 0; i < arrayLength; i++)
        {
            var operand = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(root[i], options);
            if (operand is null)
            {
                throw new JsonException(
                    $"Expected: Valid expression for + operand at index {i}, Actual: null"
                );
            }
            operands[i] = operand;
        }

        return new JsonExpressionSymbolAdd<TIn, TOut>(operands);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionSymbolAdd<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
