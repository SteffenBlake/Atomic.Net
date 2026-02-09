using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets the left border of this entity.
/// </summary>
[JsonConverter(typeof(FlexBorderLeftBehaviorConverter))]
public readonly record struct FlexBorderLeftBehavior(float Value);

