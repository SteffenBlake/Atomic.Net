using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionIf.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionIfConverter<TIn, TOut> : JsonConverter<IJsonExpressionIf<TIn, TOut>>
{
    public override IJsonExpressionIf<TIn, TOut>? Read(
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
        if (arrayLength != 3)
        {
            throw new JsonException(
                $"Expected: Array with exactly 3 elements for if operator (condition, then, else), Actual: Array with {arrayLength} elements"
            );
        }

        // Simple ternary only: [condition, thenBranch, elseBranch]
        var condition = JsonSerializer.Deserialize<IJsonExpression<TIn, bool>>(root[0], options);
        var thenBranch = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(root[1], options);
        var elseBranch = JsonSerializer.Deserialize<IJsonExpression<TIn, TOut>>(root[2], options);

        if (condition is null || thenBranch is null || elseBranch is null)
        {
            throw new JsonException(
                $"Expected: Valid expressions for if condition, then, and else branches, Actual: One or more are null"
            );
        }

        return new JsonExpressionIf<TIn, TOut>(condition, thenBranch, elseBranch);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionIf<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
