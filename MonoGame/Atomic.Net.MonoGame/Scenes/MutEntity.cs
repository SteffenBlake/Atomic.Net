using System.Text.Json.Nodes;

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
    string? Parent,
    string? FlexAlignItems,
    string? FlexAlignSelf,
    float? FlexBorderBottom,
    float? FlexBorderLeft,
    float? FlexBorderRight,
    float? FlexBorderTop,
    string? FlexDirection,
    float? FlexGrow,
    string? FlexWrap,
    int? FlexZOverride,
    MutFlexHeight? FlexHeight,
    string? FlexJustifyContent,
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
    string? FlexPositionType,
    MutFlexWidth? FlexWidth
);
