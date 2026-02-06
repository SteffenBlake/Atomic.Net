using FlexLayoutSharp;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets how this entity aligns itself along the cross axis.
/// </summary>
[JsonConverter(typeof(FlexAlignSelfBehaviorConverter))]
public readonly record struct FlexAlignSelfBehavior(Align Value);

