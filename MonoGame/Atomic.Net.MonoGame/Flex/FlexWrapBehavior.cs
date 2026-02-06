using FlexLayoutSharp;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets whether the entity wraps its children.
/// </summary>
[JsonConverter(typeof(FlexWrapBehaviorConverter))]
public readonly record struct FlexWrapBehavior(Wrap Value);

