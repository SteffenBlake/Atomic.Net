using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets the right margin of this entity.
/// </summary>
[JsonConverter(typeof(FlexMarginRightBehaviorConverter))]
public readonly record struct FlexMarginRightBehavior(float Value);

