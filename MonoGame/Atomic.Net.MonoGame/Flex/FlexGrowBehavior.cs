using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets the grow factor for this entity in a flex layout.
/// </summary>
[JsonConverter(typeof(FlexGrowBehaviorConverter))]
public readonly record struct FlexGrowBehavior(float Value);

