using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Do step that executes a SceneCommand immediately with deltaTime context.
/// </summary>
public readonly record struct DoStep(
    [property: JsonRequired]
    SceneCommand Do
);
