using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Sequence step that interpolates from a start value to an end value over a duration.
/// Executes a command per-frame with the interpolated value.
/// </summary>
public readonly record struct TweenStep(
    float From,
    float To,
    float Duration,
    SceneCommand Do
);
