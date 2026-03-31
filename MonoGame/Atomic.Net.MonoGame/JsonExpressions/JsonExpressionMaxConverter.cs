using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionMax.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionMaxConverter<TIn, TOut> : JsonConverter<IJsonExpressionMax<TIn, TOut>>
{
    public override IJsonExpressionMax<TIn, TOut>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (typeof(TOut) != typeof(float))
        {
            throw new JsonException(
                $"'max' operator requires TOut=float, got TOut={typeof(TOut).Name}"
            );
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Expected: Array for max operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength < 1)
        {
            throw new JsonException(
                $"Expected: Array with at least 1 element for max operator, Actual: Array with {arrayLength} elements"
            );
        }

        var operands = new IJsonExpression<TIn, TOut>[arrayLength];
        for (int i = 0; i < arrayLength; i++)
        {
            var operand = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(root[i], options);
            if (operand is null)
            {
                throw new JsonException(
                    $"Expected: Valid expression for max operand at index {i}, Actual: null"
                );
            }
            operands[i] = operand;
        }

        return new JsonExpressionMax<TIn, TOut>(operands);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionMax<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
