using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets the right padding of this entity.
/// </summary>
[JsonConverter(typeof(FlexPaddingRightBehaviorConverter))]
public readonly record struct FlexPaddingRightBehavior(float Value);

