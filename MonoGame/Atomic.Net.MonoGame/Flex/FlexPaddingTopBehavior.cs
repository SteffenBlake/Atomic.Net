using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets the top padding of this entity.
/// </summary>
[JsonConverter(typeof(FlexPaddingTopBehaviorConverter))]
public readonly record struct FlexPaddingTopBehavior(float Value);

