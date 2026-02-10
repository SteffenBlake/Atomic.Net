using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets the bottom margin of this entity.
/// </summary>
[JsonConverter(typeof(FlexMarginBottomBehaviorConverter))]
public readonly record struct FlexMarginBottomBehavior(float Value);

