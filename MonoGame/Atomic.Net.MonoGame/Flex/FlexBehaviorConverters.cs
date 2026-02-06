using System.Text.Json;
using System.Text.Json.Serialization;
using FlexLayoutSharp;

namespace Atomic.Net.MonoGame.Flex;

public class FlexWrapBehaviorConverter : JsonConverter<FlexWrapBehavior>
{
    public override FlexWrapBehavior Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (Enum.TryParse<Wrap>(value, true, out var wrap))
            {
                return new FlexWrapBehavior(wrap);
            }
            throw new JsonException($"Invalid Wrap value: {value}");
        }

        throw new JsonException($"Expected string for FlexWrap, got {reader.TokenType}");
    }

    public override void Write(
        Utf8JsonWriter writer,
        FlexWrapBehavior value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value.ToString());
    }
}

public class FlexJustifyContentBehaviorConverter : JsonConverter<FlexJustifyContentBehavior>
{
    public override FlexJustifyContentBehavior Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (Enum.TryParse<Justify>(value, true, out var justify))
            {
                return new FlexJustifyContentBehavior(justify);
            }
            throw new JsonException($"Invalid Justify value: {value}");
        }

        throw new JsonException($"Expected string for FlexJustifyContent, got {reader.TokenType}");
    }

    public override void Write(
        Utf8JsonWriter writer,
        FlexJustifyContentBehavior value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value.ToString());
    }
}

public class FlexAlignItemsBehaviorConverter : JsonConverter<FlexAlignItemsBehavior>
{
    public override FlexAlignItemsBehavior Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (Enum.TryParse<Align>(value, true, out var align))
            {
                return new FlexAlignItemsBehavior(align);
            }
            throw new JsonException($"Invalid Align value: {value}");
        }

        throw new JsonException($"Expected string for FlexAlignItems, got {reader.TokenType}");
    }

    public override void Write(
        Utf8JsonWriter writer,
        FlexAlignItemsBehavior value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value.ToString());
    }
}

public class FlexAlignSelfBehaviorConverter : JsonConverter<FlexAlignSelfBehavior>
{
    public override FlexAlignSelfBehavior Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (Enum.TryParse<Align>(value, true, out var align))
            {
                return new FlexAlignSelfBehavior(align);
            }
            throw new JsonException($"Invalid Align value: {value}");
        }

        throw new JsonException($"Expected string for FlexAlignSelf, got {reader.TokenType}");
    }

    public override void Write(
        Utf8JsonWriter writer,
        FlexAlignSelfBehavior value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value.ToString());
    }
}

public class AlignContentBehaviorConverter : JsonConverter<AlignContentBehavior>
{
    public override AlignContentBehavior Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (Enum.TryParse<Align>(value, true, out var align))
            {
                return new AlignContentBehavior(align);
            }
            throw new JsonException($"Invalid Align value: {value}");
        }

        throw new JsonException($"Expected string for AlignContent, got {reader.TokenType}");
    }

    public override void Write(
        Utf8JsonWriter writer,
        AlignContentBehavior value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value.ToString());
    }
}

public class FlexPositionTypeBehaviorConverter : JsonConverter<FlexPositionTypeBehavior>
{
    public override FlexPositionTypeBehavior Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (Enum.TryParse<PositionType>(value, true, out var positionType))
            {
                return new FlexPositionTypeBehavior(positionType);
            }
            throw new JsonException($"Invalid PositionType value: {value}");
        }

        throw new JsonException($"Expected string for FlexPositionType, got {reader.TokenType}");
    }

    public override void Write(
        Utf8JsonWriter writer,
        FlexPositionTypeBehavior value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value.ToString());
    }
}
