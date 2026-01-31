using System.Text.Json.Serialization;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Delay step that pauses sequence execution for a specified duration.
/// </summary>
public readonly record struct DelayStep(
    [property: JsonRequired]
    float Duration
);
