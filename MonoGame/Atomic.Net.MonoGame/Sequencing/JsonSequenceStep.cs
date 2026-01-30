using System.Text.Json.Serialization;
using dotVariant;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Variant type representing different sequence step types.
/// Each step type has specific execution semantics.
/// </summary>
[Variant]
[JsonConverter(typeof(JsonSequenceStepConverter))]
public readonly partial struct JsonSequenceStep
{
    static partial void VariantOf(
        DelayStep delay,
        DoStep @do,
        TweenStep tween,
        RepeatStep repeat
    );
}
