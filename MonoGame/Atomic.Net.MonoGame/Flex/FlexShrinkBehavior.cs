using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets the shrink factor for this entity in a flex layout.
/// </summary>
[JsonConverter(typeof(FlexShrinkBehaviorConverter))]
public readonly record struct FlexShrinkBehavior(float Value);

