using System.Text.Json.Nodes;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating a transform anchor component (x, y, or z).
/// Maps to JSON: { "anchor": "x" }
/// Applies the value to transform.anchor.x
/// </summary>
public readonly record struct JsonTransformAnchorTarget(string Anchor)
{
    public void Apply(JsonNode transform, JsonNode value) 
    {
        if (transform is not JsonObject transformObj)
        {
            return;
        }

        // Get or create the "anchor" object inside transform
        if (!transformObj.TryGetPropertyValue("anchor", out var anchorNode) || anchorNode is not JsonObject anchorObj)
        {
            anchorObj = new JsonObject();
            transformObj["anchor"] = anchorObj;
        }

        // Set the component (x/y/z) inside the anchor object
        anchorObj[Anchor] = value;
    }
}
