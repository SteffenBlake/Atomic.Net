using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Tween step that interpolates from a start value to an end value over a duration,
/// executing a command per-frame with the interpolated value.
/// </summary>
public readonly record struct TweenStep(
    [property: JsonRequired]
    float From,
    [property: JsonRequired]
    float To,
    [property: JsonRequired]
    float Duration,
    [property: JsonRequired]
    SceneCommand Do
);
