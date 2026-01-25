using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.Selectors;

namespace Atomic.Net.MonoGame.Scenes;

public readonly record struct JsonRule(
    EntitySelector From,
    JsonNode Where,
    SceneCommand Do
);
