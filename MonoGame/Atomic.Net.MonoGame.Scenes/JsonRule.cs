using System.Text.Json.Nodes;

namespace Atomic.Net.MonoGame.Scenes;

public readonly record struct JsonRule(
    EntitySelectorV2 From,
    JsonNode Where,
    SceneCommand Do
);
