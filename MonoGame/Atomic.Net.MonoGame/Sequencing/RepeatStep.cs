using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Sequence step that repeatedly executes a command at a specified interval until a condition is met.
/// </summary>
public readonly record struct RepeatStep(
    float Every,
    JsonNode Until,
    SceneCommand Do
);
