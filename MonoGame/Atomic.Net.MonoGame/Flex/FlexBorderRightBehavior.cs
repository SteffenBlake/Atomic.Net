using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets the right border of this entity.
/// </summary>
[JsonConverter(typeof(FlexBorderRightBehaviorConverter))]
public readonly record struct FlexBorderRightBehavior(float Value);

