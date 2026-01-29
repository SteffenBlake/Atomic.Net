using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Sequencing;

/// <summary>
/// Runs sub-command every interval until condition met.
/// </summary>
public readonly record struct RepeatStep(float Every, JsonNode Until, SceneCommand Do);
