using System.Text.Json.Serialization;
using dotVariant;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Variant type representing a single step in a sequence.
/// Can be a delay, do, tween, or repeat step.
/// </summary>
[Variant]
[JsonConverter(typeof(JsonSequenceStepConverter))]
public readonly partial struct JsonSequenceStep
{
    static partial void VariantOf(
        DelayStep delay,
        DoStep doStep,
        TweenStep tween,
        RepeatStep repeat
    );
}
