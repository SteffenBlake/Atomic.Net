using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// JSON converter for JsonExpressionLinqAdd.
/// </summary>
/// <typeparam name="TIn">Input data type</typeparam>
/// <typeparam name="TOut">Output type produced by the expression</typeparam>
public sealed class JsonExpressionLinqAddConverter<TIn, TOut> : JsonConverter<IJsonExpressionLinqAdd<TIn, TOut>>
{
    public override IJsonExpressionLinqAdd<TIn, TOut>? Read(
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
                $"Expected: Array for linqAdd operator, Actual: {root.ValueKind}"
            );
        }

        var arrayLength = root.GetArrayLength();
        if (arrayLength != 2)
        {
            throw new JsonException(
                $"Expected: Array with 2 elements for linqAdd operator, Actual: Array with {arrayLength} elements"
            );
        }

        // Derive element type from TOut (which is the array type TElement[])
        var elementType = typeof(TOut).GetElementType() ??
            throw new JsonException(
                $"Expected: Array type for linqAdd, Actual: {typeof(TOut).Name}"
            );

        var itemExprType = typeof(IJsonExpression<,>).MakeGenericType(typeof(TIn), elementType);
        var arrayExprType = typeof(IJsonExpression<,>).MakeGenericType(typeof(TIn), typeof(TOut));

        var item = JsonSerializer.Deserialize(root[0], itemExprType, options) ??
            throw new JsonException(
                $"Expected: Valid expression for linqAdd item, Actual: null after deserialization"
            );

        var array = JsonSerializer.Deserialize(root[1], arrayExprType, options) ??
            throw new JsonException(
                $"Expected: Valid expression for linqAdd array, Actual: null after deserialization"
            );

        var linqAddType = typeof(JsonExpressionLinqAdd<,,>).MakeGenericType(typeof(TIn), typeof(TOut), elementType);
        var constructor = linqAddType.GetConstructor([itemExprType, arrayExprType]) ??
            throw new JsonException(
                $"Unable to find constructor for JsonExpressionLinqAdd<{typeof(TIn).Name}, {typeof(TOut).Name}, {elementType.Name}>"
            );

        return (IJsonExpressionLinqAdd<TIn, TOut>)constructor.Invoke([item, array]);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IJsonExpressionLinqAdd<TIn, TOut> value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
