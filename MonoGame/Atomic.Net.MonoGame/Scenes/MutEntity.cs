using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core.JsonConverters;
using Atomic.Net.MonoGame.Selectors;
using FlexLayoutSharp;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Represents an entity's mutable state during JSON deserialization.
/// All properties are nullable to support partial mutations.
/// Matches the JSON structure produced by JsonEntityConverter.Read().
/// </summary>
public readonly record struct MutEntity(
    string? Id,
    JsonNode[]? Tags,
    Dictionary<string, JsonNode>? Properties,
    MutTransform? Transform,

    [property: JsonConverter(typeof(NullableEntitySelectorConverter))]
    EntitySelector? Parent,
    
    [property: JsonConverter(typeof(InvariantJsonStringEnumConverter<Align>))]
    Align? FlexAlignItems,
    
    [property: JsonConverter(typeof(InvariantJsonStringEnumConverter<Align>))]
    Align? FlexAlignSelf,
    float? FlexBorderBottom,
    float? FlexBorderLeft,
    float? FlexBorderRight,
    float? FlexBorderTop,
    
    [property: JsonConverter(typeof(InvariantJsonStringEnumConverter<FlexDirection>))]
    FlexDirection? FlexDirection,
    float? FlexGrow,
    
    [property: JsonConverter(typeof(InvariantJsonStringEnumConverter<Wrap>))]
    Wrap? FlexWrap,

    int? FlexZOverride,
    MutFlexHeight? FlexHeight,
    
    [property: JsonConverter(typeof(InvariantJsonStringEnumConverter<Justify>))]
    Justify? FlexJustifyContent,
    
    float? FlexMarginBottom,
    float? FlexMarginLeft,
    float? FlexMarginRight,
    float? FlexMarginTop,
    float? FlexPaddingBottom,
    float? FlexPaddingLeft,
    float? FlexPaddingRight,
    float? FlexPaddingTop,
    MutFlexPositionBottom? FlexPositionBottom,
    MutFlexPositionLeft? FlexPositionLeft,
    MutFlexPositionRight? FlexPositionRight,
    MutFlexPositionTop? FlexPositionTop,

    [property: JsonConverter(typeof(InvariantJsonStringEnumConverter<PositionType>))]
    PositionType? FlexPositionType,

    MutFlexWidth? FlexWidth
);
