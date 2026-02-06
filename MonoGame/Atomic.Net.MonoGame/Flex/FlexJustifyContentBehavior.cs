using FlexLayoutSharp;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets how the entity justifies its children along the main axis.
/// </summary>
[JsonConverter(typeof(FlexJustifyContentBehaviorConverter))]
public readonly record struct FlexJustifyContentBehavior(Justify Value);

