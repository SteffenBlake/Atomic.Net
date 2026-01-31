using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Repeat step that executes a command every interval until a condition is met.
/// Exposes 'elapsed' time to the condition and command context.
/// </summary>
public readonly record struct RepeatStep(
    [property: JsonRequired]
    float Every,
    [property: JsonRequired]
    JsonNode Until,
    [property: JsonRequired]
    SceneCommand Do
);
