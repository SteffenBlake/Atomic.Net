using System.Text.Json.Nodes;

namespace Atomic.Net.MonoGame.Scenes.JsonTargets;

/// <summary>
/// Target for mutating a transform scale component (x, y, or z).
/// Maps to JSON: { "scale": "x" }
/// Applies the value to transform.scale.x
/// </summary>
public readonly record struct JsonTransformScaleTarget(string Scale)
{
    public void Apply(JsonNode transform, JsonNode value) 
    {
        if (transform is not JsonObject transformObj)
        {
            return;
        }

        // Get or create the "scale" object inside transform
        if (!transformObj.TryGetPropertyValue("scale", out var scaleNode) || scaleNode is not JsonObject scaleObj)
        {
            scaleObj = new JsonObject();
            transformObj["scale"] = scaleObj;
        }

        // Set the component (x/y/z) inside the scale object
        scaleObj[Scale] = value;
    }
}
