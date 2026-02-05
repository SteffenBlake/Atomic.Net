using FlexLayoutSharp;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets how the entity aligns its children along the cross axis.
/// </summary>
[JsonConverter(typeof(FlexAlignItemsBehaviorConverter))]
public readonly record struct FlexAlignItemsBehavior(Align Value);

