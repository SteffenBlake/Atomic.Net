using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Executes a SceneCommand immediately with deltaTime context.
/// </summary>
public readonly record struct DoStep(SceneCommand Do);
