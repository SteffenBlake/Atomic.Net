using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets the left padding of this entity.
/// </summary>
[JsonConverter(typeof(FlexPaddingLeftBehaviorConverter))]
public readonly record struct FlexPaddingLeftBehavior(float Value);

