using System.Text.Json.Nodes;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating a transform anchor component (x, y, or z).
/// Maps to JSON: { "anchor": "x" }
/// </summary>
public readonly record struct JsonTransformAnchorTarget(string Anchor)
{
    public void Apply(JsonNode transform, JsonNode value) 
    {
        if (transform is not JsonObject transformObj)
        {
            return;
        }

        transformObj[Anchor] = value;
    }
}
