using FlexLayoutSharp;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets the position type of this entity.
/// </summary>
[JsonConverter(typeof(FlexPositionTypeBehaviorConverter))]
public readonly record struct FlexPositionTypeBehavior(PositionType Value);

