using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Sequence step that executes a command immediately with deltaTime context.
/// </summary>
public readonly record struct DoStep(SceneCommand Do);
