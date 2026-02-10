using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets the bottom border of this entity.
/// </summary>
[JsonConverter(typeof(FlexBorderBottomBehaviorConverter))]
public readonly record struct FlexBorderBottomBehavior(float Value);

