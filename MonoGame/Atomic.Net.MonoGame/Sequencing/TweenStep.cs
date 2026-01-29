using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Interpolates from 'from' to 'to' over a duration, running sub-commands per-frame.
/// </summary>
public readonly record struct TweenStep(float From, float To, float Duration, SceneCommand Do);
