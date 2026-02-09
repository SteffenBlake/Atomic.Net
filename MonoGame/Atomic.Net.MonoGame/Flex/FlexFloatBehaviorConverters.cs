using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// JSON converter for FlexPaddingLeftBehavior that allows direct float values.
/// Converts JSON "flexPaddingLeft": 10 to new FlexPaddingLeftBehavior(10).
/// </summary>
public class FlexPaddingLeftBehaviorConverter : JsonConverter<FlexPaddingLeftBehavior>
{
    public override FlexPaddingLeftBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexPaddingLeftBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexPaddingLeftBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexPaddingLeftBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class FlexPaddingRightBehaviorConverter : JsonConverter<FlexPaddingRightBehavior>
{
    public override FlexPaddingRightBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexPaddingRightBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexPaddingRightBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexPaddingRightBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class FlexPaddingTopBehaviorConverter : JsonConverter<FlexPaddingTopBehavior>
{
    public override FlexPaddingTopBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexPaddingTopBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexPaddingTopBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexPaddingTopBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class FlexPaddingBottomBehaviorConverter : JsonConverter<FlexPaddingBottomBehavior>
{
    public override FlexPaddingBottomBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexPaddingBottomBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexPaddingBottomBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexPaddingBottomBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class FlexMarginLeftBehaviorConverter : JsonConverter<FlexMarginLeftBehavior>
{
    public override FlexMarginLeftBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexMarginLeftBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexMarginLeftBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexMarginLeftBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class FlexMarginRightBehaviorConverter : JsonConverter<FlexMarginRightBehavior>
{
    public override FlexMarginRightBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexMarginRightBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexMarginRightBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexMarginRightBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class FlexMarginTopBehaviorConverter : JsonConverter<FlexMarginTopBehavior>
{
    public override FlexMarginTopBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexMarginTopBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexMarginTopBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexMarginTopBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class FlexMarginBottomBehaviorConverter : JsonConverter<FlexMarginBottomBehavior>
{
    public override FlexMarginBottomBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexMarginBottomBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexMarginBottomBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexMarginBottomBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class FlexBorderLeftBehaviorConverter : JsonConverter<FlexBorderLeftBehavior>
{
    public override FlexBorderLeftBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexBorderLeftBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexBorderLeftBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexBorderLeftBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class FlexBorderRightBehaviorConverter : JsonConverter<FlexBorderRightBehavior>
{
    public override FlexBorderRightBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexBorderRightBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexBorderRightBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexBorderRightBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class FlexBorderTopBehaviorConverter : JsonConverter<FlexBorderTopBehavior>
{
    public override FlexBorderTopBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexBorderTopBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexBorderTopBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexBorderTopBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class FlexBorderBottomBehaviorConverter : JsonConverter<FlexBorderBottomBehavior>
{
    public override FlexBorderBottomBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new FlexBorderBottomBehavior(reader.GetSingle());
        }
        throw new JsonException($"Expected number for FlexBorderBottomBehavior, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, FlexBorderBottomBehavior value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
