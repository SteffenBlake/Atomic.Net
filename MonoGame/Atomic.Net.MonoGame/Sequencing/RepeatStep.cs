using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Repeat step that executes a command every interval until a condition is met.
/// Exposes 'elapsed' time to the condition and command context.
/// </summary>
public readonly record struct RepeatStep(
    float Every,
    JsonNode Until,
    SceneCommand Do
);
