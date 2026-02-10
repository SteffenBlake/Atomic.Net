using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets the bottom padding of this entity.
/// </summary>
[JsonConverter(typeof(FlexPaddingBottomBehaviorConverter))]
public readonly record struct FlexPaddingBottomBehavior(float Value);

