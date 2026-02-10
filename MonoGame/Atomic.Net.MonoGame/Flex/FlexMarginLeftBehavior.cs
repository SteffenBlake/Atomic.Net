using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets the left margin of this entity.
/// </summary>
[JsonConverter(typeof(FlexMarginLeftBehaviorConverter))]
public readonly record struct FlexMarginLeftBehavior(float Value);

