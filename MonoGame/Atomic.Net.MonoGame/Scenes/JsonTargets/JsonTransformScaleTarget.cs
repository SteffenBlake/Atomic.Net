using System.Text.Json.Nodes;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating a transform scale component (x, y, or z).
/// Maps to JSON: { "scale": "x" }
/// </summary>
public readonly record struct JsonTransformScaleTarget(string Scale)
{
    public void Apply(JsonNode transform, JsonNode value) 
    {
        if (transform is not JsonObject transformObj)
        {
            return;
        }

        transformObj[Scale] = value;
    }
}
