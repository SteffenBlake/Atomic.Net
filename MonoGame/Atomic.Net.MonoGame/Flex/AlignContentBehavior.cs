using FlexLayoutSharp;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets how content is aligned when there is extra space in the cross axis.
/// </summary>
[JsonConverter(typeof(AlignContentBehaviorConverter))]
public readonly record struct AlignContentBehavior(Align Value);

