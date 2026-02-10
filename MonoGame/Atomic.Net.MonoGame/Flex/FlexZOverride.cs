using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Overrides the rendering Z Index of a flex item
/// </summary>
[JsonConverter(typeof(FlexZOverrideConverter))]
public readonly record struct FlexZOverride(int ZIndex);

