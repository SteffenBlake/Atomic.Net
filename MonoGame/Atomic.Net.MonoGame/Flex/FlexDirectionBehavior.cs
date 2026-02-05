using FlexLayoutSharp;
using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Flex;

/// <summary>
/// Sets the flex direction of an entity.
/// </summary>
[JsonConverter(typeof(FlexDirectionBehaviorConverter))]
public readonly record struct FlexDirectionBehavior(FlexDirection Value);

