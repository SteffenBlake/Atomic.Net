using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets the top border of this entity.
/// </summary>
[JsonConverter(typeof(FlexBorderTopBehaviorConverter))]
public readonly record struct FlexBorderTopBehavior(float Value);

