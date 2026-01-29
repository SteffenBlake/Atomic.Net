using System.Text.Json.Serialization;
using dotVariant;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Variant type representing different sequence step types.
/// </summary>
[Variant]
[JsonConverter(typeof(JsonSequenceStepConverter))]
public readonly partial struct JsonSequenceStep
{
    static partial void VariantOf(DelayStep delay, DoStep doStep, TweenStep tween, RepeatStep repeat);
}
