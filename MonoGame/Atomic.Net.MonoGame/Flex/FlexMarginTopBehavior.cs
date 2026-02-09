using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets the top margin of this entity.
/// </summary>
[JsonConverter(typeof(FlexMarginTopBehaviorConverter))]
public readonly record struct FlexMarginTopBehavior(float Value);

